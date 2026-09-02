namespace SkillsetsBackend.Application.SystemAdmins.Commands.ResetSystemAdminPassword;

/// <summary>A SuperAdmin setting a System Administrator's password directly. No current password is
/// asked for - this is an administrative reset by someone above them, not a self-service change.</summary>
public record ResetSystemAdminPasswordCommand(string NewPassword, string ConfirmPassword);
