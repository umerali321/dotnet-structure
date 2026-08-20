using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.RoleManagement.Commands.SetUserPermissionOverrides;

/// <summary>SuperAdmin only - lets one specific person's effective permissions diverge from their
/// role's defaults (e.g. one Manager loses "Create Employees" while every other Manager keeps it).
/// The baseline to diff against is the union of BaselineRoleNames (whichever role checkboxes the
/// caller had checked in the same dialog, e.g. just "Manager" or both "Student" and "Manager") - NOT
/// a globally-resolved "current role", which could belong to a different company than the one this
/// edit is scoped to. Only the difference from that baseline is persisted as UserPermissionOverride
/// rows - a permission left matching it gets no row at all, so it keeps following the role
/// automatically if the role's own permissions change later.</summary>
public class SetUserPermissionOverridesCommandHandler
{
    private readonly IRoleRepository _repository;
    private readonly IPermissionService _permissionService;

    public SetUserPermissionOverridesCommandHandler(IRoleRepository repository, IPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task Handle(int userId, SetUserPermissionOverridesCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can edit an individual user's permissions.");
        }

        var catalog = await _repository.ListPermissionsAsync(cancellationToken);
        var keyToId = catalog.ToDictionary(p => p.PermissionKey, p => p.PermissionId);

        var roleDefaultIds = new HashSet<int>();
        foreach (var roleName in command.BaselineRoleNames.Select(Roles.Normalize).Distinct())
        {
            var keys = await _permissionService.GetEffectivePermissionKeysForRoleAsync(roleName, cancellationToken);
            foreach (var key in keys)
            {
                if (keyToId.TryGetValue(key, out var id))
                {
                    roleDefaultIds.Add(id);
                }
            }
        }

        var desiredIds = command.PermissionIds.ToHashSet();

        var overrides = new Dictionary<int, bool>();
        foreach (var permission in catalog)
        {
            var desired = desiredIds.Contains(permission.PermissionId);
            var roleDefault = roleDefaultIds.Contains(permission.PermissionId);
            if (desired != roleDefault)
            {
                overrides[permission.PermissionId] = desired;
            }
        }

        await _repository.ReplaceUserPermissionOverridesAsync(userId, overrides, caller.Email, cancellationToken);
    }
}
