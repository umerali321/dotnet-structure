using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Settings.Commands.TestSmtpConnection;

public class TestSmtpConnectionCommandHandler
{
    private readonly ISmtpSettingsRepository _repository;
    private readonly ISecretProtector _secretProtector;
    private readonly ISmtpConnectionTester _connectionTester;
    private readonly IPermissionService _permissionService;

    public TestSmtpConnectionCommandHandler(
        ISmtpSettingsRepository repository, ISecretProtector secretProtector, ISmtpConnectionTester connectionTester,
        IPermissionService permissionService)
    {
        _repository = repository;
        _secretProtector = secretProtector;
        _connectionTester = connectionTester;
        _permissionService = permissionService;
    }

    public async Task<TestSmtpConnectionResultDto> Handle(TestSmtpConnectionCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageEmail, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to test the SMTP connection.");
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
