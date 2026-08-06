using FluentValidation;
using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using AuthenticationFailedException = SkillsetsBackend.Application.Common.Exceptions.AuthenticationFailedException;

namespace SkillsetsBackend.Application.Auth.Commands.Login;

public class LoginCommandHandler
{
    private readonly IValidator<LoginCommand> _validator;
    private readonly ISuperAdminAuthenticator _authenticator;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IValidator<LoginCommand> validator,
        ISuperAdminAuthenticator authenticator,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _validator = validator;
        _authenticator = authenticator;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResultDto> Handle(LoginCommand command, string? ipAddress, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var identity = _authenticator.Validate(command.Email, command.Password);
        if (identity is null)
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var claims = AuthClaimsFactory.Create(identity.Id, identity.Email, identity.Role);
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(claims);
        var (refreshTokenValue, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken(refreshTokenValue, identity.Id, identity.Email, identity.Role, refreshTokenExpiresAt, ipAddress);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new AuthResultDto(accessToken, accessTokenExpiresAt, refreshTokenValue, refreshTokenExpiresAt);
    }
}
