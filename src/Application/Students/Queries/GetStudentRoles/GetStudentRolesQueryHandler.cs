using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Students.Queries.GetStudentRoles;

public class GetStudentRolesQueryHandler
{
    private readonly IUserDirectory _userDirectory;

    public GetStudentRolesQueryHandler(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    public async Task<IReadOnlyList<string>> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        await StudentAuthorization.EnsureCanViewStudentAsync(caller, userId, _userDirectory, cancellationToken);

        var companies = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);

        return companies
            .Select(c => Roles.Normalize(c.RoleName))
            .Distinct()
            .ToList();
    }
}
