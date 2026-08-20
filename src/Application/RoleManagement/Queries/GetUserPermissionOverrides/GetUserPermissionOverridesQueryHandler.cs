using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.Interfaces;

namespace SkillsetsBackend.Application.RoleManagement.Queries.GetUserPermissionOverrides;

public record UserPermissionOverrideDto(int PermissionId, bool IsGranted);

/// <summary>SuperAdmin only - the raw per-user override rows, with no role baseline merged in. Used
/// by the per-company Roles dialog to compute the permission checklist against whichever role
/// checkboxes are shown there, rather than the person's globally-resolved "current role".</summary>
public class GetUserPermissionOverridesQueryHandler
{
    private readonly IRoleRepository _repository;

    public GetUserPermissionOverridesQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<UserPermissionOverrideDto>> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view an individual user's permission overrides.");
        }

        var overrides = await _repository.GetUserPermissionOverridesAsync(userId, cancellationToken);
        return overrides.Select(kv => new UserPermissionOverrideDto(kv.Key, kv.Value)).ToList();
    }
}
