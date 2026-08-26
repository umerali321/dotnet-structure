using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.CourseLibrary.Interfaces;

public record CourseTakenListOptions(
    int Page,
    int PageSize,
    int? OnlyUserId,
    IReadOnlyCollection<int>? RestrictToCompanyIds,
    string? StudentNameSearch = null,
    string? CourseTitleSearch = null);

public interface ICourseTakenRepository
{
    /// <summary>The most recent record (active or completed) for this exact student+course pair -
    /// used to detect "already taken this course before" vs. "resuming the current active one". A
    /// student can retake a completed course, so more than one row per pair can exist; this always
    /// returns the latest one.</summary>
    Task<CourseTaken?> FindByUserAndCourseAsync(int userId, long courseId, CancellationToken cancellationToken = default);

    Task<CourseTaken?> FindActiveByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<CourseTaken?> GetByIdAsync(int courseTakenId, CancellationToken cancellationToken = default);

    /// <summary>Adds and saves in one step, returning false (instead of throwing) if a concurrent
    /// request already violated the active-user uniqueness constraint - the DB-level filtered unique
    /// index is the real guarantee, this just translates that failure mode into a plain result the
    /// Application layer can react to without depending on EF Core types.</summary>
    Task<bool> TryAddAsync(CourseTaken entity, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<CourseTakenDto> GetDtoAsync(int courseTakenId, CancellationToken cancellationToken = default);

    Task<PaginatedList<CourseTakenDto>> ListAsync(CourseTakenListOptions options, CancellationToken cancellationToken = default);
}
