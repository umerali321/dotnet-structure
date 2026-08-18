using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.DTOs;
using SkillsetsBackend.Application.Students.Interfaces;

namespace SkillsetsBackend.Application.Students.Queries.GetStudentById;

public class GetStudentByIdQueryHandler
{
    private readonly IStudentQueryService _queryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IStudentRepository _repository;

    public GetStudentByIdQueryHandler(IStudentQueryService queryService, IUserDirectory userDirectory, IStudentRepository repository)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
        _repository = repository;
    }

    public async Task<StudentDetailDto?> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        await StudentAuthorization.EnsureCanViewStudentAsync(caller, userId, _userDirectory, _repository, cancellationToken);

        return await _queryService.GetDetailAsync(userId, cancellationToken);
    }
}
