using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.CourseLibrary.Queries.GetMyActiveCourse;

/// <summary>Lets the UI redirect a student straight to whatever course is currently occupying
/// their single active-course slot (see CourseTaken's filtered unique index), instead of only
/// showing an error message when a new launch is blocked by it. Returns null if the caller has no
/// active course right now.</summary>
public class GetMyActiveCourseQueryHandler
{
    private readonly ICourseTakenRepository _repository;

    public GetMyActiveCourseQueryHandler(ICourseTakenRepository repository)
    {
        _repository = repository;
    }

    public async Task<CourseTakenDto?> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (caller.Role != Roles.Student || caller.DbUserId is null)
        {
            return null;
        }

        var active = await _repository.FindActiveByUserAsync(caller.DbUserId.Value, cancellationToken);
        return active is null ? null : await _repository.GetDtoAsync(active.CourseTakenId, cancellationToken);
    }
}
