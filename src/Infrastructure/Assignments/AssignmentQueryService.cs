using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Assignments;
using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Application.Assignments.Interfaces;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Assignments;

public class AssignmentQueryService : IAssignmentQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public AssignmentQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedList<AssignmentDto>> ListManagedAsync(
        IReadOnlyCollection<int>? companyIds, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Assignments.AsNoTracking().AsQueryable();
        if (companyIds is not null)
        {
            query = query.Where(a => companyIds.Contains(a.CompanyId));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var assignments = await query
            // Newest-created OR most-recently-edited first (MarkUpdated() is called on every edit -
            // see Assignment.cs) - a manager who just adjusted an assignment's employees/dates
            // should see it back at the top, not wherever CreatedAt alone would leave it.
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .ThenByDescending(a => a.AssignmentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = await BuildDtosAsync(assignments, cancellationToken);
        return new PaginatedList<AssignmentDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<AssignmentDto>> ListMineAsync(int studentUserId, CancellationToken cancellationToken = default)
    {
        var assignmentIds = await _dbContext.AssignmentEmployees
            .AsNoTracking()
            .Where(ae => ae.StudentUserId == studentUserId)
            .Select(ae => ae.AssignmentId)
            .ToListAsync(cancellationToken);

        // !IsCancelled per this method's own contract (see IAssignmentQueryService) - Cancel()
        // deliberately never removes AssignmentEmployees rows (the Manager's Ongoing Assignments
        // list still needs to show who was on a cancelled assignment), so a cancelled assignment's
        // join row to this employee stays forever and must be filtered out here instead.
        var assignments = await _dbContext.Assignments
            .AsNoTracking()
            .Where(a => assignmentIds.Contains(a.AssignmentId) && !a.IsCancelled)
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .ThenByDescending(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        return await BuildDtosAsync(assignments, cancellationToken);
    }

    public async Task<AssignmentDto?> GetDtoAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.AssignmentId == assignmentId, cancellationToken);
        if (assignment is null)
        {
            return null;
        }

        var dtos = await BuildDtosAsync([assignment], cancellationToken);
        return dtos.FirstOrDefault();
    }

    // Batch-loads employees/titles/creators/SkillTrax names for a page of assignments in a handful
    // of queries total, regardless of page size - never one query per assignment (see AGENTS.md's
    // "never N+1" rule for the Students list, applied the same way here).
    private async Task<IReadOnlyList<AssignmentDto>> BuildDtosAsync(IReadOnlyList<Assignment> assignments, CancellationToken cancellationToken)
    {
        if (assignments.Count == 0)
        {
            return [];
        }

        var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();

        var employeeRows = await (
            from ae in _dbContext.AssignmentEmployees.AsNoTracking()
            join u in _dbContext.Users.AsNoTracking() on ae.StudentUserId equals u.UserId
            where assignmentIds.Contains(ae.AssignmentId)
            select new { ae.AssignmentId, u.UserId, u.FirstName, u.LastName, u.Email })
            .ToListAsync(cancellationToken);

        var titleRows = await (
            from at in _dbContext.AssignmentTitles.AsNoTracking()
            join c in _dbContext.Courses.AsNoTracking() on at.CourseId equals c.CourseId
            where assignmentIds.Contains(at.AssignmentId)
            select new { at.AssignmentId, c.CourseId, c.CourseTitle, c.CourseUrl, c.LaunchUrl })
            .ToListAsync(cancellationToken);

        var creatorIds = assignments.Select(a => a.CreatedByUserId)
            .Concat(assignments.Where(a => a.UpdatedByUserId.HasValue).Select(a => a.UpdatedByUserId!.Value))
            .Distinct().ToList();
        var creatorRows = await _dbContext.Users
            .AsNoTracking()
            .Where(u => creatorIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.Email, u.FirstName, u.LastName })
            .ToListAsync(cancellationToken);
        var creatorEmails = creatorRows.ToDictionary(u => u.UserId, u => u.Email);
        var creatorNames = creatorRows.ToDictionary(u => u.UserId, u => $"{u.FirstName} {u.LastName}".Trim());

        // IgnoreQueryFilters() - a soft-deleted SkillTrax must still resolve its name here. Deleting
        // a SkillTrax only removes it from SkillTrax's own list/detail queries; it must never blank
        // out the name on assignments that already reference it (AssignmentTitles already holds its
        // own independent CourseId snapshot, so history stays intact either way).
        var skillTraxIds = assignments.Where(a => a.SourceSkillTraxId.HasValue).Select(a => a.SourceSkillTraxId!.Value).Distinct().ToList();
        var skillTraxNames = skillTraxIds.Count == 0
            ? new Dictionary<int, string>()
            : await _dbContext.SkillTrax.IgnoreQueryFilters().AsNoTracking().Where(s => skillTraxIds.Contains(s.SkillTraxId)).ToDictionaryAsync(s => s.SkillTraxId, s => s.Name, cancellationToken);

        // Basic per-employee, per-title progress - reuses the existing CourseTaken table (the same
        // data the "Courses Taken" screen already shows), not the blueprint's full usage-driven
        // completion model (test-score thresholds, Skillport integration), which is still out of
        // scope. One batched query for every (employee, course) pair across the whole page, not
        // one query per assignment/employee.
        var employeeIds = employeeRows.Select(e => e.UserId).Distinct().ToList();
        var courseIds = titleRows.Select(t => t.CourseId).Distinct().ToList();

        // Grouped, not ToDictionary keyed on (UserId, CourseId): a student may retake a course, and
        // the repository's own docs say more than one row per pair can exist - so keying directly
        // would throw on a duplicate key the first time anyone retook an assigned title.
        var courseTakenRows = employeeIds.Count == 0 || courseIds.Count == 0
            ? []
            : await _dbContext.CourseTakens
                .AsNoTracking()
                .Where(ct => employeeIds.Contains(ct.UserId) && courseIds.Contains(ct.CourseId))
                .Select(ct => new { ct.UserId, ct.CourseId, ct.IsActive, ct.AccessedAt })
                .ToListAsync(cancellationToken);

        var courseTakenLookup = courseTakenRows
            .GroupBy(ct => (ct.UserId, ct.CourseId))
            // Any still-active row means in progress; otherwise every attempt is finished.
            .ToDictionary(g => g.Key, g => g.Any(ct => ct.IsActive));

        // When they FIRST opened it - the earliest attempt, since a later retake says nothing about
        // whether they began on time.
        var firstAccessLookup = courseTakenRows
            .GroupBy(ct => (ct.UserId, ct.CourseId))
            .ToDictionary(g => g.Key, g => DateOnly.FromDateTime(g.Min(ct => ct.AccessedAt).UtcDateTime));

        // A title opened directly in Skillport never produces a CourseTaken row, so on that data
        // alone the person looks like they never started. The imported transcript does record it, so
        // fall back to its first-access date - otherwise anyone working outside the Course Library
        // would be reported Not started, and then Late, entirely wrongly.
        if (employeeIds.Count > 0 && courseIds.Count > 0)
        {
            var transcriptFirstAccess = await (
                from activity in _dbContext.LearningTranscriptActivities.AsNoTracking()
                join identity in _dbContext.LearningTranscriptIdentities.AsNoTracking()
                    on activity.LearningTranscriptIdentityId equals identity.LearningTranscriptIdentityId
                join asset in _dbContext.LearningTranscriptAssets.AsNoTracking()
                    on activity.AssetId equals asset.AssetId
                where activity.IsLatest
                      && identity.UserId != null
                      && asset.InternalCourseId != null
                      && employeeIds.Contains(identity.UserId!.Value)
                      && courseIds.Contains(asset.InternalCourseId!.Value)
                      && activity.FirstAccessDate != null
                select new { UserId = identity.UserId!.Value, CourseId = asset.InternalCourseId!.Value, activity.FirstAccessDate })
                .ToListAsync(cancellationToken);

            foreach (var group in transcriptFirstAccess.GroupBy(t => (t.UserId, t.CourseId)))
            {
                var earliest = group.Min(t => t.FirstAccessDate!.Value);
                // Whichever source saw them first wins - both are evidence of the same thing.
                if (!firstAccessLookup.TryGetValue(group.Key, out var existing) || earliest < existing)
                {
                    firstAccessLookup[group.Key] = earliest;
                }
            }
        }

        var titlesByAssignment = titleRows
            .GroupBy(t => t.AssignmentId)
            .ToDictionary(g => g.Key, g => g.Select(t => (t.CourseId, t.CourseTitle, t.CourseUrl, t.LaunchUrl)).ToList());

        var assignmentStartDates = assignments.ToDictionary(a => a.AssignmentId, a => a.StartDate);

        var employeesByAssignment = employeeRows
            .GroupBy(e => e.AssignmentId)
            .ToDictionary(g => g.Key, g =>
            {
                var assignmentStart = assignmentStartDates.GetValueOrDefault(g.Key);

                return (IReadOnlyList<AssignmentEmployeeDto>)g
                    .Select(e =>
                    {
                        var titleProgress = titlesByAssignment.GetValueOrDefault(g.Key, [])
                            .Select(t =>
                            {
                                var startedOn = firstAccessLookup.TryGetValue((e.UserId, t.CourseId), out var firstAccess)
                                    ? firstAccess
                                    : (DateOnly?)null;

                                return new AssignmentTitleProgressDto(
                                    t.CourseId,
                                    t.CourseTitle,
                                    DeriveProgressStatus(courseTakenLookup, e.UserId, t.CourseId).ToString(),
                                    AssignmentTiming.Derive(startedOn, assignmentStart).ToString(),
                                    startedOn);
                            })
                            .ToList();

                        // One late title makes the whole assignment late; otherwise the earliest
                        // start decides, so an employee who began before day one still reads Early.
                        var startedDates = titleProgress.Where(t => t.StartedOn.HasValue).Select(t => t.StartedOn!.Value).ToList();
                        var employeeStartedOn = startedDates.Count == 0 ? (DateOnly?)null : startedDates.Min();
                        var titleTimings = titleProgress.Select(t => Enum.Parse<AssignmentStartTiming>(t.Timing)).ToList();
                        var timing = AssignmentTiming.DeriveOverall(titleTimings, employeeStartedOn, assignmentStart);

                        return new AssignmentEmployeeDto(
                            e.UserId, e.FirstName, e.LastName, e.Email, titleProgress, timing.ToString(), employeeStartedOn);
                    })
                    .ToList();
            });

        var titleDtosByAssignment = titlesByAssignment
            .ToDictionary(kv => kv.Key, kv => (IReadOnlyList<AssignmentTitleDto>)kv.Value
                .Select(t => new AssignmentTitleDto(t.CourseId, t.CourseTitle, t.CourseUrl, t.LaunchUrl)).ToList());

        return assignments
            .Select(a => new AssignmentDto(
                a.AssignmentId,
                a.CompanyId,
                a.SourceType.ToString(),
                a.SourceSkillTraxId,
                a.SourceSkillTraxId.HasValue ? skillTraxNames.GetValueOrDefault(a.SourceSkillTraxId.Value) : null,
                a.StartDate,
                a.EndDate,
                a.IsCancelled,
                a.CancelledAt,
                a.CreatedAt,
                a.CreatedByUserId,
                creatorNames.GetValueOrDefault(a.CreatedByUserId),
                creatorEmails.GetValueOrDefault(a.CreatedByUserId),
                a.UpdatedByUserId.HasValue ? creatorNames.GetValueOrDefault(a.UpdatedByUserId.Value) : null,
                a.UpdatedByUserId.HasValue ? creatorEmails.GetValueOrDefault(a.UpdatedByUserId.Value) : null,
                a.UpdatedAt,
                employeesByAssignment.GetValueOrDefault(a.AssignmentId, []),
                titleDtosByAssignment.GetValueOrDefault(a.AssignmentId, [])))
            .ToList();
    }

    private static AssignmentTitleProgressStatus DeriveProgressStatus(
        Dictionary<(int UserId, long CourseId), bool> courseTakenLookup, int userId, long courseId)
    {
        if (!courseTakenLookup.TryGetValue((userId, courseId), out var isActive))
        {
            return AssignmentTitleProgressStatus.NotStarted;
        }

        return isActive ? AssignmentTitleProgressStatus.InProgress : AssignmentTitleProgressStatus.Completed;
    }
}
