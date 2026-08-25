using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Communications;

/// <summary>Append-only record of every outbound email attempt (Password Reset, Customer Support,
/// Assignment notifications, a SuperAdmin's test email, etc.) - written by the email-sending
/// infrastructure itself so every send is captured uniformly regardless of which feature triggered
/// it. Includes the rendered body and sender identity so a SuperAdmin can open one entry and see
/// exactly what was sent, not just that something was sent.</summary>
public class EmailLog : IAggregateRoot
{
    public int EmailLogId { get; private set; }

    public string? FromAddress { get; private set; }

    public string? FromName { get; private set; }

    public string ToAddress { get; private set; } = string.Empty;

    public string? ToName { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string? BodyHtml { get; private set; }

    /// <summary>Which feature triggered this send - "PasswordReset", "CustomerSupport",
    /// "AssignmentCreated", "AssignmentUpdated", "AssignmentCancelled", "TestEmail", or "General"
    /// for any caller that doesn't pass a specific purpose.</summary>
    public string Purpose { get; private set; } = "General";

    /// <summary>Which SMTP configuration actually sent this - one of SmtpProviderType's values, or
    /// "AppSettingsFallback" when no enabled database configuration existed.</summary>
    public string Provider { get; private set; } = string.Empty;

    public bool Success { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    private EmailLog()
    {
    }

    public static EmailLog Create(
        string? fromAddress, string? fromName, string toAddress, string? toName, string subject, string? bodyHtml,
        string purpose, string provider, bool success, string? errorMessage)
    {
        return new EmailLog
        {
            FromAddress = fromAddress,
            FromName = fromName,
            ToAddress = toAddress,
            ToName = toName,
            Subject = subject,
            BodyHtml = bodyHtml,
            Purpose = purpose,
            Provider = provider,
            Success = success,
            ErrorMessage = errorMessage,
            SentAt = DateTimeOffset.UtcNow,
        };
    }
}
