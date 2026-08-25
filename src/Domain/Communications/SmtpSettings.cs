using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Communications;

/// <summary>Singleton row (there is only ever at most one) holding the SuperAdmin-managed SMTP
/// configuration used by every outbound email in the app, in preference to the appsettings-based
/// Email:* fallback (see EmailSettings/SmtpEmailSender). The password is never stored in plaintext -
/// EncryptedPassword is produced by ISecretProtector, and this entity never exposes it decrypted;
/// only the Infrastructure email sender (which owns the protector) reads and decrypts it.</summary>
public class SmtpSettings : IAggregateRoot
{
    public int SmtpSettingsId { get; private set; }

    public string Provider { get; private set; } = SmtpProviderType.Custom;

    /// <summary>Master switch - disabled means "ignore this row, use the appsettings fallback"
    /// even though a configuration is saved, e.g. while an admin is mid-edit testing a new provider.</summary>
    public bool IsEnabled { get; private set; }

    public string Host { get; private set; } = string.Empty;

    public int Port { get; private set; }

    public bool EnableSsl { get; private set; }

    public string Username { get; private set; } = string.Empty;

    /// <summary>Null only when no password has ever been saved - a save that omits a new password
    /// keeps whatever was here before (see SetEncryptedPassword's callers).</summary>
    public string? EncryptedPassword { get; private set; }

    public string FromEmail { get; private set; } = string.Empty;

    public string FromName { get; private set; } = string.Empty;

    public string? ReplyToEmail { get; private set; }

    /// <summary>Mirrors EmailSettings.ToAddress/ToName - the inbox SendToSupportAsync delivers to.</summary>
    public string? SupportToEmail { get; private set; }

    public string? SupportToName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private SmtpSettings()
    {
    }

    public static SmtpSettings Create(
        string provider, bool isEnabled, string host, int port, bool enableSsl, string username,
        string fromEmail, string fromName, string? replyToEmail, string? supportToEmail, string? supportToName)
    {
        return new SmtpSettings
        {
            Provider = provider,
            IsEnabled = isEnabled,
            Host = host,
            Port = port,
            EnableSsl = enableSsl,
            Username = username,
            FromEmail = fromEmail,
            FromName = fromName,
            ReplyToEmail = replyToEmail,
            SupportToEmail = supportToEmail,
            SupportToName = supportToName,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Every field except the password - SetEncryptedPassword is separate and optional so a
    /// save that isn't changing the password never has to round-trip it through the client.</summary>
    public void UpdateConfiguration(
        string provider, bool isEnabled, string host, int port, bool enableSsl, string username,
        string fromEmail, string fromName, string? replyToEmail, string? supportToEmail, string? supportToName)
    {
        Provider = provider;
        IsEnabled = isEnabled;
        Host = host;
        Port = port;
        EnableSsl = enableSsl;
        Username = username;
        FromEmail = fromEmail;
        FromName = fromName;
        ReplyToEmail = replyToEmail;
        SupportToEmail = supportToEmail;
        SupportToName = supportToName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetEncryptedPassword(string encryptedPassword)
    {
        EncryptedPassword = encryptedPassword;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
