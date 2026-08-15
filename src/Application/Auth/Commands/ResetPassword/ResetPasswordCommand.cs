namespace SkillsetsBackend.Application.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Email);

public record ResetPasswordResultDto(bool Found);
