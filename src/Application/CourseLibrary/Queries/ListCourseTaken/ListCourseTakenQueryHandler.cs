using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.CourseLibrary.Queries.ListCourseTaken;

public class ListCourseTakenQueryHandler
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    private readonly ICourseTakenRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public ListCourseTakenQueryHandler(ICourseTakenRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<PaginatedList<CourseTakenDto>> Handle(ListCourseTakenQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = query.PageSize <= 0 ? DefaultPageSize : Math.Min(MaxPageSize, query.PageSize);

        int? onlyUserId = null;
        IReadOnlyCollection<int>? restrictToCompanyIds = null;

        if (caller.Role == Roles.Student)
        {
            if (caller.DbUserId is null)
            {
                return new PaginatedList<CourseTakenDto>([], 0, page, pageSize);
            }

            onlyUserId = caller.DbUserId.Value;
        }
        else if (caller.IsSuperAdmin)
        {
            // Unrestricted - restrictToCompanyIds stays null.
        }
        else if (caller.Role == Roles.Manager || caller.Role == Roles.CompanyAdmin)
        {
            restrictToCompanyIds = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);
        }
        else
        {
            throw new UnauthorizedAccessException("You do not have access to Course Taken records.");
        }

        return await _repository.ListAsync(
            new CourseTakenListOptions(page, pageSize, onlyUserId, restrictToCompanyIds),
            cancellationToken);
    }
}
