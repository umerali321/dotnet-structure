namespace SkillsetsBackend.Application.RoleManagement.Commands.CreateRole;

/// <summary>PermissionIds may be empty - a role with zero permissions is valid (grants nothing until edited).</summary>
public record CreateRoleCommand(string RoleName, IReadOnlyCollection<int> PermissionIds);
