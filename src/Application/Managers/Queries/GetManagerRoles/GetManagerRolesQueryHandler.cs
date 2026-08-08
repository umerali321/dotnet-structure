using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Managers.Queries.GetManagerRoles;

public class GetManagerRolesQueryHandler
{
    private readonly IUserDirectory _userDirectory;

    public GetManagerRolesQueryHandler(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    public async Task<IReadOnlyList<string>> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company managers can view manager roles.");
        }

        var companies = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);

        return companies
            .Select(c => Roles.Normalize(c.RoleName))
            .Distinct()
            .ToList();
    }
}
