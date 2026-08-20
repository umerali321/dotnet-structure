namespace SkillsetsBackend.Application.Students.Commands.AddEmployeeRole;

/// <summary>Grants Student (Employee) to an already-existing user (typically a current Manager) at
/// the given company - a second UserCompanyRole alongside whatever role(s) they already hold, not a
/// replacement.</summary>
public record AddEmployeeRoleCommand(int UserId, int CompanyId);
