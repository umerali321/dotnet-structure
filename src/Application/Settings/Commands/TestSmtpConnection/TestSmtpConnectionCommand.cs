namespace SkillsetsBackend.Application.Settings.Commands.TestSmtpConnection;

/// <summary>Same shape as SaveSmtpSettingsCommand so a SuperAdmin can test before saving - Password
/// is optional here too: omitted/blank falls back to whatever is already saved (see the handler),
/// so testing after a save doesn't require retyping it.</summary>
public record TestSmtpConnectionCommand(
    string Host,
    int Port,
    bool EnableSsl,
    string Username,
    string? Password);
