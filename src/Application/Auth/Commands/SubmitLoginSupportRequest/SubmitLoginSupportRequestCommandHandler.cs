using System.Net;
using System.Text;
using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Auth.Commands.SubmitLoginSupportRequest;

public class SubmitLoginSupportRequestCommandHandler
{
    private readonly IValidator<SubmitLoginSupportRequestCommand> _validator;
    private readonly IEmailSender _emailSender;
    private readonly ILoginActivityLogRepository _activityLogRepository;

    public SubmitLoginSupportRequestCommandHandler(
        IValidator<SubmitLoginSupportRequestCommand> validator,
        IEmailSender emailSender,
        ILoginActivityLogRepository activityLogRepository)
    {
        _validator = validator;
        _emailSender = emailSender;
        _activityLogRepository = activityLogRepository;
    }

    public async Task Handle(SubmitLoginSupportRequestCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var subject = $"Login help needed - {command.Name}";
        var body = BuildEmailBody(command);

        // Reply-to the submitter directly, so a support agent hitting "Reply" in their inbox
        // reaches the user rather than the Gmail account this was sent through.
        await _emailSender.SendAsync(subject, body, command.Email, command.Name, cancellationToken);

        await _activityLogRepository.AddAsync(
            LoginActivityLog.SupportRequestSubmitted(command.Name, command.Email, command.Phone, command.CompanyName, command.Message),
            cancellationToken);
        await _activityLogRepository.SaveChangesAsync(cancellationToken);
    }

    private static string BuildEmailBody(SubmitLoginSupportRequestCommand command)
    {
        var rows = new StringBuilder();
        AppendRow(rows, "Name", command.Name);
        AppendRow(rows, "SkillSets email", command.Email, isEmail: true);
        AppendRow(rows, "Phone", command.Phone);
        AppendRow(rows, "Company", command.CompanyName);
        AppendRow(rows, "Message", command.Message);

        return $$"""
            <div style="background-color:#f4f4f5;padding:32px 16px;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e7e5e3;">
                <tr>
                  <td style="background:#c81322;padding:22px 28px;">
                    <div style="color:#ffffff;font-size:18px;font-weight:700;letter-spacing:0.5px;">SKILLSETS</div>
                    <div style="color:#ffd9dc;font-size:12px;margin-top:4px;">Login help request</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:28px;">
                    <p style="margin:0 0 20px;color:#1a1918;font-size:14px;line-height:1.6;">
                      A user could not sign in to SkillSets and asked for help from the login page. Their details are below - reply to this email to respond to them directly.
                    </p>
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                      {{rows}}
                    </table>
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 28px;background:#f7f6f5;border-top:1px solid #e7e5e3;">
                    <p style="margin:0;color:#6b6663;font-size:11px;line-height:1.5;">Sent automatically from the SkillSets sign-in page.</p>
                  </td>
                </tr>
              </table>
            </div>
            """;
    }

    private static void AppendRow(StringBuilder rows, string label, string? value, bool isEmail = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var encoded = WebUtility.HtmlEncode(value);
        var valueHtml = isEmail
            ? $"""<a href="mailto:{encoded}" style="color:#c81322;text-decoration:none;">{encoded}</a>"""
            : encoded;

        rows.Append($$"""
            <tr>
              <td style="padding:10px 0;border-bottom:1px solid #e7e5e3;color:#6b6663;font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:0.4px;width:130px;vertical-align:top;white-space:nowrap;">{{label}}</td>
              <td style="padding:10px 0 10px 16px;border-bottom:1px solid #e7e5e3;color:#1a1918;font-size:14px;line-height:1.5;">{{valueHtml}}</td>
            </tr>
            """);
    }
}
