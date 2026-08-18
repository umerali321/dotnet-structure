using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Students.Queries.GetStudentRoles;

public class GetStudentRolesQueryHandler
{
    private readonly IUserDirectory _userDirectory;
    private readonly IStudentRepository _repository;

    public GetStudentRolesQueryHandler(IUserDirectory userDirectory, IStudentRepository repository)
    {
        _userDirectory = userDirectory;
        _repository = repository;
    }

    public async Task<IReadOnlyList<string>> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        await StudentAuthorization.EnsureCanViewStudentAsync(caller, userId, _userDirectory, _repository, cancellationToken);

        var companies = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);

        return companies
            .Select(c => Roles.Normalize(c.RoleName))
            .Distinct()
            .ToList();
    }
}
