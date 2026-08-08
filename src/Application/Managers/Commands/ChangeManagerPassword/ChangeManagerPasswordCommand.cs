namespace SkillsetsBackend.Application.Managers.Commands.ChangeManagerPassword;

public record ChangeManagerPasswordCommand(string NewPassword, string? CurrentPassword);
