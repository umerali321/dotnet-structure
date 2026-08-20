namespace SkillsetsBackend.Application.RoleManagement.Commands.SetUserPermissionOverrides;

/// <summary>The complete set of PermissionIds this specific user should effectively have - same
/// "full desired state, not a delta" shape as UpdateRolePermissionsCommand. The handler diffs this
/// against the union of BaselineRoleNames' default permissions (the role checkboxes shown alongside
/// this checklist in the UI, NOT the person's globally-resolved "current" role, which could be at a
/// completely different company) and only persists the difference as overrides - a permission left
/// matching that baseline gets no row at all, so it stays following the role automatically if the
/// role's own permissions change later.</summary>
public record SetUserPermissionOverridesCommand(IReadOnlyCollection<string> BaselineRoleNames, IReadOnlyCollection<int> PermissionIds);
