using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.DTOs;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Students.Queries.GetStudentCompanies;

public class GetStudentCompaniesQueryHandler
{
    private readonly IUserDirectory _userDirectory;

    public GetStudentCompaniesQueryHandler(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    public async Task<IReadOnlyList<StudentCompanyRoleDto>> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        await StudentAuthorization.EnsureCanViewStudentAsync(caller, userId, _userDirectory, cancellationToken);

        var companies = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);

        return companies
            .Select(c => new StudentCompanyRoleDto(c.CompanyId, c.CompanyName, Roles.Normalize(c.RoleName), c.StartDate, c.EndDate))
            .ToList();
    }
}
