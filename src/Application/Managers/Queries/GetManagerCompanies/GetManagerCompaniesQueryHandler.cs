using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.DTOs;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Managers.Queries.GetManagerCompanies;

public class GetManagerCompaniesQueryHandler
{
    private readonly IUserDirectory _userDirectory;

    public GetManagerCompaniesQueryHandler(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    public async Task<IReadOnlyList<ManagerCompanyDto>> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can view manager companies.");
        }

        var companies = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);

        return companies
            .Where(c => Roles.Normalize(c.RoleName) == Roles.Manager)
            .Select(c => new ManagerCompanyDto(c.CompanyId, c.CompanyName, Roles.Normalize(c.RoleName), c.StartDate, c.EndDate))
            .ToList();
    }
}
