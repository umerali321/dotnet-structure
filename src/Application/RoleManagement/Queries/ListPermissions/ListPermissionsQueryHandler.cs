using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.DTOs;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.RoleManagement.Queries.ListPermissions;

/// <summary>Read-only catalog - Permissions are never created through the API, only seeded via
/// migration (see PermissionConfiguration.cs). SuperAdmin and CompanyAdmin can view it.</summary>
public class ListPermissionsQueryHandler
{
    private readonly IRoleRepository _repository;

    public ListPermissionsQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PermissionDto>> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and Company Admins can view the permission catalog.");
        }

        return await _repository.ListPermissionsAsync(cancellationToken);
    }
}
