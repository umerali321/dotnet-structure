namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface IEmailSender
{
    /// <summary>Sends to an explicit recipient, e.g. the account holder a password was reset for.</summary>
    Task SendAsync(
        string toAddress,
        string? toName,
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sends to the configured support inbox (Email:ToAddress/ToName) - for internal
    /// notifications like a login-page support request, not for anything addressed to a user.</summary>
    Task SendToSupportAsync(
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        CancellationToken cancellationToken = default);
}
