using FluentValidation;
using Microsoft.Extensions.Logging;
using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Notifications;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using AuthenticationFailedException = SkillsetsBackend.Application.Common.Exceptions.AuthenticationFailedException;
using AccountLockedException = SkillsetsBackend.Application.Common.Exceptions.AccountLockedException;

namespace SkillsetsBackend.Application.Auth.Commands.Login;

public class LoginCommandHandler
{
    private const int MaxFailedAttempts = 10;
    private const int LockoutWindowMinutes = 15;

    private readonly IValidator<LoginCommand> _validator;
    private readonly ISuperAdminAuthenticator _superAdminAuthenticator;
    private readonly IUserDirectory _userDirectory;
    private readonly ILegacyCredentialVerifier _credentialVerifier;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILoginActivityLogRepository _loginActivityLogRepository;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly NotificationDispatcher _notifications;

    public LoginCommandHandler(
        IValidator<LoginCommand> validator,
        ISuperAdminAuthenticator superAdminAuthenticator,
        IUserDirectory userDirectory,
        ILegacyCredentialVerifier credentialVerifier,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginActivityLogRepository loginActivityLogRepository,
        ILogger<LoginCommandHandler> logger,
        NotificationDispatcher notifications)
    {
        _validator = validator;
        _superAdminAuthenticator = superAdminAuthenticator;
        _userDirectory = userDirectory;
        _credentialVerifier = credentialVerifier;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _loginActivityLogRepository = loginActivityLogRepository;
        _logger = logger;
        _notifications = notifications;
    }

    /// <summary>requestId/userAgent are for diagnostics only (see the [LOGIN] log lines below) -
    /// never anything from the credentials is logged, only a masked identifier.</summary>
    public async Task<AuthResultDto> Handle(
        LoginCommand command, string? ipAddress, string? requestId = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var maskedEmail = MaskIdentifier(command.Email);
        _logger.LogInformation(
            "[LOGIN] requestId={RequestId} email={MaskedEmail} clientIp={ClientIp} userAgent={UserAgent}",
            requestId, maskedEmail, ipAddress, userAgent);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("[LOGIN] requestId={RequestId} validation-failed returning-status=400", requestId);
            throw new AppValidationException(validationResult.Errors);
        }

        var now = DateTimeOffset.UtcNow;
        var recentFailures = await _loginActivityLogRepository.GetRecentFailedLoginTimestampsAsync(
            command.Email, now.AddMinutes(-LockoutWindowMinutes), MaxFailedAttempts, cancellationToken);

        if (recentFailures.Count >= MaxFailedAttempts)
        {
            // recentFailures is newest-first, capped at MaxFailedAttempts - the last entry is the
            // oldest of the current strike, so the lockout window rolls off from there.
            var lockoutExpiresAt = recentFailures[^1].AddMinutes(LockoutWindowMinutes);
            if (lockoutExpiresAt > now)
            {
                var minutesRemaining = Math.Max(1, (int)Math.Ceiling((lockoutExpiresAt - now).TotalMinutes));
                _logger.LogWarning(
                    "[LOGIN] requestId={RequestId} email={MaskedEmail} account-locked returning-status=423",
                    requestId, maskedEmail);
                throw new AccountLockedException(
                    $"Too many failed login attempts. Please try again in {minutesRemaining} minute(s).");
            }
        }

        var superAdmin = _superAdminAuthenticator.Validate(command.Email, command.Password);
        if (superAdmin is not null)
        {
            _logger.LogInformation(
                "[LOGIN] requestId={RequestId} email={MaskedEmail} user-found=superadmin password-valid=true",
                requestId, maskedEmail);
            var superAdminResult = await IssueTokensAsync(
                superAdmin.Id.ToString(),
                superAdmin.Email,
                superAdmin.Role,
                currentCompany: null,
                companies: [],
                ipAddress,
                cancellationToken);
            _logger.LogInformation(
                "[LOGIN] requestId={RequestId} email={MaskedEmail} token-created=true returning-status=200",
                requestId, maskedEmail);
            return superAdminResult;
        }

        var user = await _userDirectory.FindByIdentifierAsync(command.Email, cancellationToken);
        if (user is null
            || !user.IsActive
            || string.IsNullOrEmpty(user.LegacyPasswordValue)
            || !_credentialVerifier.Verify(command.Password, user.LegacyPasswordValue))
        {
            await _loginActivityLogRepository.AddAsync(LoginActivityLog.LoginFailed(command.Email), cancellationToken);
            await _loginActivityLogRepository.SaveChangesAsync(cancellationToken);

            // recentFailures was captured before this attempt (and before the lockout gate above
            // let us this far, so it's under MaxFailedAttempts) - +1 accounts for the failure just logged.
            var failedAttemptCount = recentFailures.Count + 1;
            var remainingAttempts = Math.Max(0, MaxFailedAttempts - failedAttemptCount);

            _logger.LogInformation(
                "[LOGIN] requestId={RequestId} email={MaskedEmail} user-found={UserFound} password-valid=false " +
                "login-activity-created=true returning-status=401",
                requestId, maskedEmail, user is not null);
            throw new AuthenticationFailedException("Invalid email/username or password.", failedAttemptCount, remainingAttempts);
        }

        _logger.LogInformation(
            "[LOGIN] requestId={RequestId} email={MaskedEmail} userId={UserId} user-found=true password-valid=true",
            requestId, maskedEmail, user.UserId);

        var activeCompanyRoles = await _userDirectory.GetActiveCompanyRolesAsync(user.UserId, cancellationToken);
        var (role, currentCompany, companies) = CompanyContextResolver.Resolve(activeCompanyRoles);

        // Zero active company roles is ambiguous by itself - it's also what a never-assigned account
        // looks like. Only reject when the user HAS at least one membership row that's now entirely
        // inactive (their own membership, or their company, was deactivated) - a genuine 2+-company
        // user always has active rows here and never reaches this branch.
        if (role == CompanyContextResolver.UnassignedRole
            && companies.Count == 0
            && await _userDirectory.HasAnyCompanyRoleAsync(user.UserId, cancellationToken))
        {
            await _loginActivityLogRepository.AddAsync(LoginActivityLog.LoginFailed(command.Email), cancellationToken);
            await _loginActivityLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[LOGIN] requestId={RequestId} email={MaskedEmail} userId={UserId} company-inactive-or-expired=true " +
                "login-activity-created=true returning-status=401",
                requestId, maskedEmail, user.UserId);
            throw new AuthenticationFailedException(
                "Your company account has been deactivated or its trial/license has expired. Contact your administrator.");
        }

        var result = await IssueTokensAsync(
            user.UserId.ToString(),
            user.Email ?? user.Username ?? command.Email,
            role,
            currentCompany,
            companies,
            ipAddress,
            cancellationToken);

        _logger.LogInformation(
            "[LOGIN] requestId={RequestId} userId={UserId} role={Role} companyCount={CompanyCount} token-created=true returning-status=200",
            requestId, user.UserId, role, companies.Count);

        // The sign-in notification email was removed at the customer's request. Sign-ins are still
        // recorded in LoginActivityLogs (System Logs), which is where access is reviewed - the email
        // added nothing that page does not already show, and generated one message per employee
        // login.
        return result;
    }

    private async Task<AuthResultDto> IssueTokensAsync(
        string userId,
        string email,
        string role,
        CompanyDto? currentCompany,
        IReadOnlyList<CompanyDto> companies,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var claims = AuthClaimsFactory.Create(userId, email, role, currentCompany?.CompanyId, currentCompany?.CompanyName);
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(claims);
        var (refreshTokenValue, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken(
            refreshTokenValue,
            userId,
            email,
            role,
            currentCompany?.CompanyId,
            currentCompany?.CompanyName,
            refreshTokenExpiresAt,
            ipAddress);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new AuthResultDto(accessToken, accessTokenExpiresAt, refreshTokenValue, refreshTokenExpiresAt, role, currentCompany, companies);
    }

    /// <summary>Keeps enough of the identifier to be useful for support ("which user complained")
    /// without writing a full email/username to the log - e.g. "pk***@cgsinc.com".</summary>
    private static string MaskIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return "(empty)";
        }

        var atIndex = identifier.IndexOf('@');
        if (atIndex <= 0)
        {
            return identifier.Length <= 2 ? "**" : $"{identifier[..2]}***";
        }

        var localPart = identifier[..atIndex];
        var domainPart = identifier[atIndex..];
        var visible = localPart.Length <= 2 ? localPart : localPart[..2];
        return $"{visible}***{domainPart}";
    }
}
