using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Settings.Commands.TestSmtpConnection;

public class TestSmtpConnectionCommandHandler
{
    private readonly ISmtpSettingsRepository _repository;
    private readonly ISecretProtector _secretProtector;
    private readonly ISmtpConnectionTester _connectionTester;

    public TestSmtpConnectionCommandHandler(
        ISmtpSettingsRepository repository, ISecretProtector secretProtector, ISmtpConnectionTester connectionTester)
    {
        _repository = repository;
        _secretProtector = secretProtector;
        _connectionTester = connectionTester;
    }

    public async Task<TestSmtpConnectionResultDto> Handle(TestSmtpConnectionCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can test SMTP settings.");
        }

        if (string.IsNullOrWhiteSpace(command.Host) || string.IsNullOrWhiteSpace(command.Username))
        {
            throw new AppValidationException([new ValidationFailure(nameof(command.Host), "Host and username are required to test a connection.")]);
        }

        var password = command.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            var saved = await _repository.GetAsync(cancellationToken);
            if (string.IsNullOrEmpty(saved?.EncryptedPassword))
            {
                throw new AppValidationException([new ValidationFailure(nameof(command.Password), "Enter a password, or save one first, before testing.")]);
            }

            password = _secretProtector.Unprotect(saved.EncryptedPassword);
        }

        var result = await _connectionTester.TestAsync(command.Host, command.Port, command.EnableSsl, command.Username, password, cancellationToken);
        return new TestSmtpConnectionResultDto(result.Success, result.Message);
    }
}
