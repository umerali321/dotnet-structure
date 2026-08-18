using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.DTOs;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.RoleManagement.Queries.ListRoles;

public class ListRolesQueryHandler
{
    private readonly IRoleRepository _repository;

    public ListRolesQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<RoleSummaryDto>> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and Company Admins can view roles.");
        }

        return await _repository.ListRolesAsync(cancellationToken);
    }
}
