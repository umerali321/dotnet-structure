using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.DTOs;
using SkillsetsBackend.Application.Students.Interfaces;

namespace SkillsetsBackend.Application.Students.Queries.GetStudentById;

public class GetStudentByIdQueryHandler
{
    private readonly IStudentQueryService _queryService;
    private readonly IUserDirectory _userDirectory;

    public GetStudentByIdQueryHandler(IStudentQueryService queryService, IUserDirectory userDirectory)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
    }

    public async Task<StudentDetailDto?> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        await StudentAuthorization.EnsureCanViewStudentAsync(caller, userId, _userDirectory, cancellationToken);

        return await _queryService.GetDetailAsync(userId, cancellationToken);
    }
}
