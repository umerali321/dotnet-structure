namespace SkillsetsBackend.Application.Settings.Commands.SaveSmtpSettings;

/// <summary>Password is optional on every save after the first - null/empty means "keep whatever
/// password is already saved", so the client never has to know or re-submit it just to change an
/// unrelated field like FromName.</summary>
public record SaveSmtpSettingsCommand(
    string Provider,
    bool IsEnabled,
    string Host,
    int Port,
    bool EnableSsl,
    string Username,
    string? Password,
    string FromEmail,
    string FromName,
    string? ReplyToEmail,
    string? SupportToEmail,
    string? SupportToName);
