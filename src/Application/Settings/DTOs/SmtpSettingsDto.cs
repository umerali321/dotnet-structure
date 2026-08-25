namespace SkillsetsBackend.Application.Settings.DTOs;

/// <summary>Never carries the password/encrypted value, in either direction - only HasPassword,
/// so the client can render "a password is saved" without ever seeing or round-tripping it.</summary>
public record SmtpSettingsDto(
    int SmtpSettingsId,
    string Provider,
    bool IsEnabled,
    string Host,
    int Port,
    bool EnableSsl,
    string Username,
    bool HasPassword,
    string FromEmail,
    string FromName,
    string? ReplyToEmail,
    string? SupportToEmail,
    string? SupportToName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record TestSmtpConnectionResultDto(bool Success, string Message);

/// <summary>List-row shape - deliberately excludes BodyHtml, which can be large and isn't needed
/// until a specific row is opened (see EmailLogDetailDto/GetEmailLogDetailQuery).</summary>
public record EmailLogDto(
    int EmailLogId,
    string ToAddress,
    string? ToName,
    string Subject,
    string Purpose,
    string Provider,
    bool Success,
    string? ErrorMessage,
    DateTimeOffset SentAt);

/// <summary>Everything EmailLogDto has, plus the sender identity and the full rendered body - only
/// fetched on demand when a SuperAdmin opens one row's "View" action.</summary>
public record EmailLogDetailDto(
    int EmailLogId,
    string? FromAddress,
    string? FromName,
    string ToAddress,
    string? ToName,
    string Subject,
    string? BodyHtml,
    string Purpose,
    string Provider,
    bool Success,
    string? ErrorMessage,
    DateTimeOffset SentAt);
