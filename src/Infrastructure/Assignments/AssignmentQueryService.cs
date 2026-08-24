using Microsoft.EntityFrameworkCore;
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
            .OrderByDescending(a => a.CreatedAt)
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

        var assignments = await _dbContext.Assignments
            .AsNoTracking()
            .Where(a => assignmentIds.Contains(a.AssignmentId))
            .OrderByDescending(a => a.CreatedAt)
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

        var creatorIds = assignments.Select(a => a.CreatedByUserId).Distinct().ToList();
        var creatorEmails = await _dbContext.Users
            .AsNoTracking()
            .Where(u => creatorIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.Email, cancellationToken);

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
        var courseTakenLookup = employeeIds.Count == 0 || courseIds.Count == 0
            ? new Dictionary<(int UserId, long CourseId), bool>()
            : await _dbContext.CourseTakens
                .AsNoTracking()
                .Where(ct => employeeIds.Contains(ct.UserId) && courseIds.Contains(ct.CourseId))
                .ToDictionaryAsync(ct => (ct.UserId, ct.CourseId), ct => ct.IsActive, cancellationToken);

        var titlesByAssignment = titleRows
            .GroupBy(t => t.AssignmentId)
            .ToDictionary(g => g.Key, g => g.Select(t => (t.CourseId, t.CourseTitle, t.CourseUrl, t.LaunchUrl)).ToList());

        var employeesByAssignment = employeeRows
            .GroupBy(e => e.AssignmentId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AssignmentEmployeeDto>)g
                .Select(e => new AssignmentEmployeeDto(
                    e.UserId, e.FirstName, e.LastName, e.Email,
                    titlesByAssignment.GetValueOrDefault(g.Key, [])
                        .Select(t => new AssignmentTitleProgressDto(t.CourseId, t.CourseTitle, DeriveProgressStatus(courseTakenLookup, e.UserId, t.CourseId).ToString()))
                        .ToList()))
                .ToList());

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
                creatorEmails.GetValueOrDefault(a.CreatedByUserId),
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
