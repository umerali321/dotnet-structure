namespace SkillsetsBackend.Application.Students.Commands.CreateStudent;

// AlsoCreateManager: also grants this brand-new person a Manager role at the same company, in the
// same request - equivalent to creating the Employee then separately calling
// AddManagerRoleCommand for the same userId.
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
    bool CreateInSkillport = false,
    bool AlsoCreateManager = false);
