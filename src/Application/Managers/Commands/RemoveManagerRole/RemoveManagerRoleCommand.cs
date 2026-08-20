namespace SkillsetsBackend.Application.Managers.Commands.RemoveManagerRole;

/// <summary>Revokes Manager from a user at the given company - any other role(s) they hold (here or
/// at other companies) are untouched. Refused if this is their only active role anywhere, so a
/// checkbox toggle can never lock someone out of the whole system.</summary>
public record RemoveManagerRoleCommand(int UserId, int CompanyId);
