using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Infrastructure.Options;

namespace SkillsetsBackend.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public SmtpEmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task SendAsync(
        string toAddress,
        string? toName,
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        CancellationToken cancellationToken = default) =>
        SendCoreAsync(toAddress, toName, subject, bodyHtml, replyToEmail, replyToName, cancellationToken);

    public Task SendToSupportAsync(
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        CancellationToken cancellationToken = default) =>
        SendCoreAsync(_settings.ToAddress, _settings.ToName, subject, bodyHtml, replyToEmail, replyToName, cancellationToken);

    private async Task SendCoreAsync(
        string toAddress,
        string? toName,
        string subject,
        string bodyHtml,
        string? replyToEmail,
        string? replyToName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.Username)
            || string.IsNullOrWhiteSpace(_settings.Password) || string.IsNullOrWhiteSpace(toAddress))
        {
            throw new InvalidOperationException("Email settings are not configured. Set Email:SmtpHost, Email:Username, Email:Password (and Email:ToAddress for support notifications) in configuration.");
        }

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = bodyHtml,
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(toAddress, toName));

        if (!string.IsNullOrWhiteSpace(replyToEmail))
        {
            message.ReplyToList.Add(new MailAddress(replyToEmail, replyToName));
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
