using System.Net.Mail;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Settings.Commands.SendTestEmail;

public class SendTestEmailCommandHandler
{
    private readonly IEmailSender _emailSender;
    private readonly IPermissionService _permissionService;

    public SendTestEmailCommandHandler(IEmailSender emailSender,
        IPermissionService permissionService)
    {
        _emailSender = emailSender;
        _permissionService = permissionService;
    }

    public async Task Handle(SendTestEmailCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageEmail, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to send a test email.");
        }

        if (string.IsNullOrWhiteSpace(command.ToAddress) || !MailAddress.TryCreate(command.ToAddress, out _))
        {
            throw new AppValidationException([new ValidationFailure(nameof(command.ToAddress), "Enter a valid email address.")]);
        }

        const string body = """
            <p>This is a test email from SkillSets, sent using your currently configured SMTP settings.</p>
            <p>If you received this, your SMTP configuration is working correctly.</p>
            """;

        await _emailSender.SendAsync(command.ToAddress, null, "SkillSets SMTP Test Email", body, purpose: "TestEmail", cancellationToken: cancellationToken);
    }
}
