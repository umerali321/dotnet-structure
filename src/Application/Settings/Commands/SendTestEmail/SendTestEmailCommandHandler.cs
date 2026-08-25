using System.Net.Mail;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Settings.Commands.SendTestEmail;

public class SendTestEmailCommandHandler
{
    private readonly IEmailSender _emailSender;

    public SendTestEmailCommandHandler(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task Handle(SendTestEmailCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can send a test email.");
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
