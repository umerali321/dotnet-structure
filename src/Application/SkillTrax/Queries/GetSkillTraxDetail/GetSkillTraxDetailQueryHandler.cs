using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SkillTrax.DTOs;
using SkillsetsBackend.Application.SkillTrax.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.SkillTrax.Queries.GetSkillTraxDetail;

public class GetSkillTraxDetailQueryHandler
{
    private readonly ISkillTraxQueryService _queryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public GetSkillTraxDetailQueryHandler(ISkillTraxQueryService queryService, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<SkillTraxDto> Handle(int skillTraxId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.SkillTrax.View, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view SkillTrax.");
        }

        var detail = await _queryService.GetDetailAsync(skillTraxId, cancellationToken)
            ?? throw new NotFoundException("SkillTrax", skillTraxId);

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, detail.CompanyId, _userDirectory, cancellationToken);
        }

        return detail;
    }
}
