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

    public async Task SendAsync(
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.Username)
            || string.IsNullOrWhiteSpace(_settings.Password) || string.IsNullOrWhiteSpace(_settings.ToAddress))
        {
            throw new InvalidOperationException("Email settings are not configured. Set Email:SmtpHost, Email:Username, Email:Password, and Email:ToAddress in configuration.");
        }

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = bodyHtml,
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(_settings.ToAddress, _settings.ToName));

        if (!string.IsNullOrWhiteSpace(replyToEmail))
        {
            message.ReplyToList.Add(new MailAddress(replyToEmail, replyToName));
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
