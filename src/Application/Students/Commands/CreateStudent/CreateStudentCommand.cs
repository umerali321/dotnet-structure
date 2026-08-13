namespace SkillsetsBackend.Application.Students.Commands.CreateStudent;

public record CreateStudentCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Username,
    string Password,
    string? StudentType,
    int CompanyId,
    DateOnly? StartDate,
    bool CreateInSkillport = false);
