namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface IEmailSender
{
    /// <summary>Sends to an explicit recipient, e.g. the account holder a password was reset for.
    /// purpose is a short label ("PasswordReset", "AssignmentCreated", ...) recorded on the Email
    /// History row this send produces - purely descriptive, callers that don't pass one just show up
    /// as "General" in that history.</summary>
    Task SendAsync(
        string toAddress,
        string? toName,
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        string purpose = "General",
        CancellationToken cancellationToken = default);

    /// <summary>Sends to the configured support inbox (the active SMTP configuration's
    /// SupportToEmail/SupportToName, or Email:ToAddress/ToName as a fallback) - for internal
    /// notifications like a login-page support request, not for anything addressed to a user.</summary>
    Task SendToSupportAsync(
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        string purpose = "General",
        CancellationToken cancellationToken = default);
}
