namespace SkillsetsBackend.Application.Students.Commands.RemoveEmployeeRole;

/// <summary>Revokes Employee (Student) from a user at the given company - any other role(s) they
/// hold (here or at other companies) are untouched. Refused if this is their only active role
/// anywhere, so a checkbox toggle can never lock someone out of the whole system.</summary>
public record RemoveEmployeeRoleCommand(int UserId, int CompanyId);
