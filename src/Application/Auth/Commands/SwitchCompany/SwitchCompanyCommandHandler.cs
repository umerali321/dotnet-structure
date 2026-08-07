using FluentValidation;
using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Auth.Commands.SwitchCompany;

public class SwitchCompanyCommandHandler
{
    private readonly IValidator<SwitchCompanyCommand> _validator;
    private readonly IUserDirectory _userDirectory;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public SwitchCompanyCommandHandler(
        IValidator<SwitchCompanyCommand> validator,
        IUserDirectory userDirectory,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _validator = validator;
        _userDirectory = userDirectory;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResultDto> Handle(
        SwitchCompanyCommand command,
        string currentUserId,
        string currentEmail,
        string currentRole,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (currentRole == Roles.SuperAdmin)
        {
            throw new UnauthorizedAccessException("SuperAdmin is not scoped to a single company.");
        }

        var userId = int.Parse(currentUserId);
        var target = await _userDirectory.GetActiveCompanyRoleAsync(userId, command.CompanyId, cancellationToken);
        if (target is null)
        {
            throw new UnauthorizedAccessException("You do not have access to that company.");
        }

        var role = Roles.Normalize(target.RoleName);
        var currentCompany = new CompanyDto(target.CompanyId, target.CompanyName, role);

        var claims = AuthClaimsFactory.Create(currentUserId, currentEmail, role, currentCompany.CompanyId, currentCompany.CompanyName);
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(claims);
        var (refreshTokenValue, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken(
            refreshTokenValue,
            currentUserId,
            currentEmail,
            role,
            currentCompany.CompanyId,
            currentCompany.CompanyName,
            refreshTokenExpiresAt,
            ipAddress);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        var allCompanies = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);
        var companies = allCompanies
            .Select(x => new CompanyDto(x.CompanyId, x.CompanyName, Roles.Normalize(x.RoleName)))
            .ToList();

        return new AuthResultDto(accessToken, accessTokenExpiresAt, refreshTokenValue, refreshTokenExpiresAt, role, currentCompany, companies);
    }
}
