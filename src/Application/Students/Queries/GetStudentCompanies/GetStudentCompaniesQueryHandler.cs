using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.DTOs;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Students.Queries.GetStudentCompanies;

public class GetStudentCompaniesQueryHandler
{
    private readonly IUserDirectory _userDirectory;
    private readonly IStudentRepository _repository;

    public GetStudentCompaniesQueryHandler(IUserDirectory userDirectory, IStudentRepository repository)
    {
        _userDirectory = userDirectory;
        _repository = repository;
    }

    public async Task<IReadOnlyList<StudentCompanyRoleDto>> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        await StudentAuthorization.EnsureCanViewStudentAsync(caller, userId, _userDirectory, _repository, cancellationToken);

        var companies = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);

        return companies
            .Select(c => new StudentCompanyRoleDto(c.CompanyId, c.CompanyCode, c.CompanyName, Roles.Normalize(c.RoleName), c.StartDate, c.EndDate))
            .ToList();
    }
}
