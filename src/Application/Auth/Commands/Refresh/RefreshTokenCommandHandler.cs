using FluentValidation;
using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using AuthenticationFailedException = SkillsetsBackend.Application.Common.Exceptions.AuthenticationFailedException;

namespace SkillsetsBackend.Application.Auth.Commands.Refresh;

public class RefreshTokenCommandHandler
{
    private readonly IValidator<RefreshTokenCommand> _validator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserDirectory _userDirectory;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IValidator<RefreshTokenCommand> validator,
        IRefreshTokenRepository refreshTokenRepository,
        IUserDirectory userDirectory,
        ITokenService tokenService)
    {
        _validator = validator;
        _refreshTokenRepository = refreshTokenRepository;
        _userDirectory = userDirectory;
        _tokenService = tokenService;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand command, string? ipAddress, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var existing = await _refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);
        if (existing is null || !existing.IsActive)
        {
            throw new AuthenticationFailedException("Invalid or expired refresh token.");
        }

        string role;
        CompanyDto? currentCompany;
        IReadOnlyList<CompanyDto> companies;

        if (existing.Role == Roles.SuperAdmin)
        {
            role = Roles.SuperAdmin;
            currentCompany = null;
            companies = [];
        }
        else
        {
            var userId = int.Parse(existing.UserId);

            if (existing.CompanyId is not null)
            {
                var stillActive = await _userDirectory.GetActiveCompanyRoleAsync(
                    userId, existing.CompanyId.Value, existing.Role, cancellationToken);
                if (stillActive is null)
                {
                    throw new AuthenticationFailedException("Company access has changed. Please sign in again.");
                }

                role = Roles.Normalize(stillActive.RoleName);
                currentCompany = new CompanyDto(stillActive.CompanyId, stillActive.CompanyCode, stillActive.CompanyName, role);
                companies = (await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken))
                    .Select(x => new CompanyDto(x.CompanyId, x.CompanyCode, x.CompanyName, Roles.Normalize(x.RoleName)))
                    .ToList();
            }
            else
            {
                var activeCompanyRoles = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);
                (role, currentCompany, companies) = CompanyContextResolver.Resolve(activeCompanyRoles);
            }
        }

        var claims = AuthClaimsFactory.Create(existing.UserId, existing.Email, role, currentCompany?.CompanyId, currentCompany?.CompanyName);
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(claims);
        var (newRefreshTokenValue, newRefreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken(
            newRefreshTokenValue,
            existing.UserId,
            existing.Email,
            role,
            currentCompany?.CompanyId,
            currentCompany?.CompanyName,
            newRefreshTokenExpiresAt,
            ipAddress);

        existing.Revoke(ipAddress, newRefreshTokenValue);
        await _refreshTokenRepository.UpdateAsync(existing, cancellationToken);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return new AuthResultDto(accessToken, accessTokenExpiresAt, newRefreshTokenValue, newRefreshTokenExpiresAt, role, currentCompany, companies);
    }
}
