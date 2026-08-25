using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Communications;
using SkillsetsBackend.Infrastructure.Options;

namespace SkillsetsBackend.Infrastructure.Email;

/// <summary>Resolves which SMTP configuration to send through on every call: the SuperAdmin-managed
/// database configuration (Settings > Email Settings) when one exists and is enabled, otherwise the
/// appsettings Email:* section as a fallback - preserving the original, pre-database behavior for any
/// environment that hasn't configured one yet. Every attempt (success or failure, either source) is
/// recorded to Email History via IEmailLogRepository.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _fallbackSettings;
    private readonly ISmtpSettingsRepository _smtpSettingsRepository;
    private readonly ISecretProtector _secretProtector;
    private readonly IEmailLogRepository _emailLogRepository;

    public SmtpEmailSender(
        IOptions<EmailSettings> fallbackSettings,
        ISmtpSettingsRepository smtpSettingsRepository,
        ISecretProtector secretProtector,
        IEmailLogRepository emailLogRepository)
    {
        _fallbackSettings = fallbackSettings.Value;
        _smtpSettingsRepository = smtpSettingsRepository;
        _secretProtector = secretProtector;
        _emailLogRepository = emailLogRepository;
    }

    public Task SendAsync(
        string toAddress,
        string? toName,
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        string purpose = "General",
        CancellationToken cancellationToken = default) =>
        SendCoreAsync(toAddress, toName, subject, bodyHtml, replyToEmail, replyToName, purpose, cancellationToken);

    public async Task SendToSupportAsync(
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        string purpose = "General",
        CancellationToken cancellationToken = default)
    {
        var (resolved, _) = await ResolveActiveConfigAsync(cancellationToken);
        await SendCoreAsync(resolved.ToAddress, resolved.ToName, subject, bodyHtml, replyToEmail, replyToName, purpose, cancellationToken);
    }

    private async Task SendCoreAsync(
        string toAddress,
        string? toName,
        string subject,
        string bodyHtml,
        string? replyToEmail,
        string? replyToName,
        string purpose,
        CancellationToken cancellationToken)
    {
        var (resolved, providerLabel) = await ResolveActiveConfigAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(resolved.SmtpHost) || string.IsNullOrWhiteSpace(resolved.Username)
            || string.IsNullOrWhiteSpace(resolved.Password) || string.IsNullOrWhiteSpace(toAddress))
        {
            var configError = "Email settings are not configured. Set them from Settings > Email Settings, or Email:SmtpHost/Username/Password in configuration.";
            await LogAsync(resolved.FromAddress, resolved.FromName, toAddress, toName, subject, bodyHtml, purpose, providerLabel, success: false, configError, cancellationToken);
            throw new InvalidOperationException(configError);
        }

        try
        {
            using var client = new SmtpClient(resolved.SmtpHost, resolved.SmtpPort)
            {
                EnableSsl = resolved.EnableSsl,
                Credentials = new NetworkCredential(resolved.Username, resolved.Password),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(resolved.FromAddress, resolved.FromName),
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
            await LogAsync(resolved.FromAddress, resolved.FromName, toAddress, toName, subject, bodyHtml, purpose, providerLabel, success: true, errorMessage: null, cancellationToken);
        }
        catch (Exception ex)
        {
            await LogAsync(resolved.FromAddress, resolved.FromName, toAddress, toName, subject, bodyHtml, purpose, providerLabel, success: false, ex.Message, cancellationToken);
            throw;
        }
    }

    /// <summary>The active database configuration, decrypted, when one exists and is enabled and
    /// complete; otherwise the appsettings fallback, unchanged from before this feature existed.</summary>
    private async Task<(ResolvedSmtpConfig Config, string ProviderLabel)> ResolveActiveConfigAsync(CancellationToken cancellationToken)
    {
        var dbSettings = await _smtpSettingsRepository.GetAsync(cancellationToken);

        if (dbSettings is { IsEnabled: true } && !string.IsNullOrWhiteSpace(dbSettings.Host) && !string.IsNullOrWhiteSpace(dbSettings.EncryptedPassword))
        {
            var password = _secretProtector.Unprotect(dbSettings.EncryptedPassword);
            return (
                new ResolvedSmtpConfig(
                    dbSettings.Host, dbSettings.Port, dbSettings.Username, password, dbSettings.EnableSsl,
                    dbSettings.FromEmail, dbSettings.FromName,
                    dbSettings.SupportToEmail ?? _fallbackSettings.ToAddress, dbSettings.SupportToName ?? _fallbackSettings.ToName),
                dbSettings.Provider);
        }

        return (
            new ResolvedSmtpConfig(
                _fallbackSettings.SmtpHost, _fallbackSettings.SmtpPort, _fallbackSettings.Username, _fallbackSettings.Password,
                _fallbackSettings.EnableSsl, _fallbackSettings.FromAddress, _fallbackSettings.FromName,
                _fallbackSettings.ToAddress, _fallbackSettings.ToName),
            "AppSettingsFallback");
    }

    private async Task LogAsync(
        string? fromAddress, string? fromName, string toAddress, string? toName, string subject, string bodyHtml,
        string purpose, string provider, bool success, string? errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            await _emailLogRepository.AddAsync(
                EmailLog.Create(fromAddress, fromName, toAddress, toName, subject, bodyHtml, purpose, provider, success, errorMessage),
                cancellationToken);
        }
        catch
        {
            // Best-effort - Email History is a diagnostic aid, never a reason to fail (or mask the
            // real error from) the actual email send this call is about.
        }
    }

    private record ResolvedSmtpConfig(
        string SmtpHost, int SmtpPort, string Username, string Password, bool EnableSsl,
        string FromAddress, string FromName, string ToAddress, string ToName);
}
