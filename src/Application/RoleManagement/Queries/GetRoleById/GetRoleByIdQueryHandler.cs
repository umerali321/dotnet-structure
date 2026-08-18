using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.DTOs;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.RoleManagement.Queries.GetRoleById;

public class GetRoleByIdQueryHandler
{
    private readonly IRoleRepository _repository;

    public GetRoleByIdQueryHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<RoleDto?> Handle(byte roleId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and Company Admins can view a role's permissions.");
        }

        return await _repository.GetRoleByIdAsync(roleId, cancellationToken);
    }
}
