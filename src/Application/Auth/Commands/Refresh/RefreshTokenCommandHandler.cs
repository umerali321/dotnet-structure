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
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IValidator<RefreshTokenCommand> validator,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService)
    {
        _validator = validator;
        _refreshTokenRepository = refreshTokenRepository;
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

        var claims = AuthClaimsFactory.Create(existing.UserId, existing.Email, existing.Role);
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(claims);
        var (newRefreshTokenValue, newRefreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken(newRefreshTokenValue, existing.UserId, existing.Email, existing.Role, newRefreshTokenExpiresAt, ipAddress);

        existing.Revoke(ipAddress, newRefreshTokenValue);
        await _refreshTokenRepository.UpdateAsync(existing, cancellationToken);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return new AuthResultDto(accessToken, accessTokenExpiresAt, newRefreshTokenValue, newRefreshTokenExpiresAt);
    }
}
