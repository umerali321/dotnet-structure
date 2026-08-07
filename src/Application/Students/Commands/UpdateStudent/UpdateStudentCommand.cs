namespace SkillsetsBackend.Application.Students.Commands.UpdateStudent;

public record UpdateStudentCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Username,
    string? StudentType);
