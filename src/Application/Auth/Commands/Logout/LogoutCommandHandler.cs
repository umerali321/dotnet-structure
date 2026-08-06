using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Auth.Commands.Logout;

public class LogoutCommandHandler
{
    private readonly IValidator<LogoutCommand> _validator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IValidator<LogoutCommand> validator, IRefreshTokenRepository refreshTokenRepository)
    {
        _validator = validator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(LogoutCommand command, string? ipAddress, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var existing = await _refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);
        if (existing is not null && existing.IsActive)
        {
            existing.Revoke(ipAddress);
            await _refreshTokenRepository.UpdateAsync(existing, cancellationToken);
        }
    }
}
