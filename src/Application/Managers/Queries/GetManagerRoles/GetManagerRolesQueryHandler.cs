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
        if (!caller.IsPlatformAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can view manager roles.");
        }

        var companies = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);

        return companies
            .Select(c => Roles.Normalize(c.RoleName))
            .Distinct()
            .ToList();
    }
}
