namespace SkillsetsBackend.Application.Managers.Commands.AddManagerRole;

/// <summary>Grants Manager to an already-existing user (typically a current Employee) at the given
/// company - a second UserCompanyRole alongside whatever role(s) they already hold, not a
/// replacement.</summary>
public record AddManagerRoleCommand(int UserId, int CompanyId);
