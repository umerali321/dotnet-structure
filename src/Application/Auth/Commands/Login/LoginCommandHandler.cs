using FluentValidation;
using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Application.Auth.Interfaces;
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

    public LoginCommandHandler(
        IValidator<LoginCommand> validator,
        ISuperAdminAuthenticator superAdminAuthenticator,
        IUserDirectory userDirectory,
        ILegacyCredentialVerifier credentialVerifier,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginActivityLogRepository loginActivityLogRepository)
    {
        _validator = validator;
        _superAdminAuthenticator = superAdminAuthenticator;
        _userDirectory = userDirectory;
        _credentialVerifier = credentialVerifier;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _loginActivityLogRepository = loginActivityLogRepository;
    }

    public async Task<AuthResultDto> Handle(LoginCommand command, string? ipAddress, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
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
                throw new AccountLockedException(
                    $"Too many failed login attempts. Please try again in {minutesRemaining} minute(s).");
            }
        }

        var superAdmin = _superAdminAuthenticator.Validate(command.Email, command.Password);
        if (superAdmin is not null)
        {
            return await IssueTokensAsync(
                superAdmin.Id.ToString(),
                superAdmin.Email,
                superAdmin.Role,
                currentCompany: null,
                companies: [],
                ipAddress,
                cancellationToken);
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
            throw new AuthenticationFailedException("Invalid email/username or password.", failedAttemptCount, remainingAttempts);
        }

        var activeCompanyRoles = await _userDirectory.GetActiveCompanyRolesAsync(user.UserId, cancellationToken);
        var (role, currentCompany, companies) = CompanyContextResolver.Resolve(activeCompanyRoles);

        return await IssueTokensAsync(
            user.UserId.ToString(),
            user.Email ?? user.Username ?? command.Email,
            role,
            currentCompany,
            companies,
            ipAddress,
            cancellationToken);
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
}
