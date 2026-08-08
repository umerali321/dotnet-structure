using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.DTOs;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Managers.Queries.GetManagerById;

public class GetManagerByIdQueryHandler
{
    private readonly IManagerQueryService _queryService;
    private readonly IUserDirectory _userDirectory;

    public GetManagerByIdQueryHandler(IManagerQueryService queryService, IUserDirectory userDirectory)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
    }

    public async Task<ManagerListItemDto?> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company managers can view managers.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageStudentAsync(caller, userId, _userDirectory, cancellationToken);
        }

        return await _queryService.GetAsync(userId, cancellationToken);
    }
}
