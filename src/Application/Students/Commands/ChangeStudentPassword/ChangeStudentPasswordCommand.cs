namespace SkillsetsBackend.Application.Students.Commands.ChangeStudentPassword;

/// <summary>CurrentPassword is required only when a Student is changing their own password.</summary>
public record ChangeStudentPasswordCommand(string NewPassword, string? CurrentPassword);
