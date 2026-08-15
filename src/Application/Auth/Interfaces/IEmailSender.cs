namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface IEmailSender
{
    Task SendAsync(
        string subject,
        string bodyHtml,
        string? replyToEmail = null,
        string? replyToName = null,
        CancellationToken cancellationToken = default);
}
