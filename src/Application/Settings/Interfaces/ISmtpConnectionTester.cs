namespace SkillsetsBackend.Application.Settings.Interfaces;

public record SmtpConnectionTestResult(bool Success, string Message);

/// <summary>Verifies host/port/credentials are reachable and accepted WITHOUT sending an email -
/// distinct from "send a test email" (see SendTestEmailCommand), which actually delivers a message.
/// System.Net.Mail.SmtpClient has no "authenticate only" API, so this speaks raw SMTP (EHLO / STARTTLS
/// / AUTH LOGIN / QUIT) directly over a socket.</summary>
public interface ISmtpConnectionTester
{
    Task<SmtpConnectionTestResult> TestAsync(
        string host, int port, bool enableSsl, string username, string password, CancellationToken cancellationToken = default);
}
