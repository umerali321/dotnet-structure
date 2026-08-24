using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.SkillTrax.DTOs;
using SkillsetsBackend.Application.SkillTrax.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Assignments;

public class SkillTraxQueryService : ISkillTraxQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public SkillTraxQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SkillTraxSummaryDto>> ListAsync(
        IReadOnlyCollection<int>? companyIds, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SkillTrax.AsNoTracking().AsQueryable();
        if (companyIds is not null)
        {
            query = query.Where(s => companyIds.Contains(s.CompanyId));
        }

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.SkillTraxId, s.Name, s.CompanyId, s.CreatedAt, s.CreatedByUserId, s.UpdatedByUserId, s.UpdatedAt })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return [];
        }

        var ids = items.Select(x => x.SkillTraxId).ToList();
        var companyIdsForNames = items.Select(x => x.CompanyId).Distinct().ToList();
        var companyNames = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => companyIdsForNames.Contains(c.CompanyId))
            .ToDictionaryAsync(c => c.CompanyId, c => c.CompanyName, cancellationToken);

        var userIds = items.Select(x => x.CreatedByUserId)
            .Concat(items.Where(x => x.UpdatedByUserId.HasValue).Select(x => x.UpdatedByUserId!.Value))
            .Distinct().ToList();
        var userRows = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.Email, u.FirstName, u.LastName })
            .ToListAsync(cancellationToken);
        var userEmails = userRows.ToDictionary(u => u.UserId, u => u.Email);
        var userNames = userRows.ToDictionary(u => u.UserId, u => $"{u.FirstName} {u.LastName}".Trim());
        var counts = await _dbContext.SkillTraxCourses
            .AsNoTracking()
            .Where(c => ids.Contains(c.SkillTraxId))
            .GroupBy(c => c.SkillTraxId)
            .Select(g => new { SkillTraxId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SkillTraxId, x => x.Count, cancellationToken);

        // Distinct employees across every non-cancelled assignment sourced from each SkillTrax -
        // "how many people currently have this bundle assigned."
        var memberCounts = await (
            from a in _dbContext.Assignments.AsNoTracking()
            join ae in _dbContext.AssignmentEmployees.AsNoTracking() on a.AssignmentId equals ae.AssignmentId
            where a.SourceSkillTraxId != null && ids.Contains(a.SourceSkillTraxId!.Value) && !a.IsCancelled
            select new { SkillTraxId = a.SourceSkillTraxId!.Value, ae.StudentUserId })
            .Distinct()
            .GroupBy(x => x.SkillTraxId)
            .Select(g => new { SkillTraxId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SkillTraxId, x => x.Count, cancellationToken);

        return items
            .Select(x => new SkillTraxSummaryDto(
                x.SkillTraxId, x.Name, x.CompanyId, companyNames.GetValueOrDefault(x.CompanyId), x.CreatedAt,
                counts.GetValueOrDefault(x.SkillTraxId), memberCounts.GetValueOrDefault(x.SkillTraxId),
                userNames.GetValueOrDefault(x.CreatedByUserId), userEmails.GetValueOrDefault(x.CreatedByUserId),
                x.UpdatedByUserId.HasValue ? userNames.GetValueOrDefault(x.UpdatedByUserId.Value) : null,
                x.UpdatedByUserId.HasValue ? userEmails.GetValueOrDefault(x.UpdatedByUserId.Value) : null,
                x.UpdatedAt))
            .ToList();
    }

    public async Task<SkillTraxDto?> GetDetailAsync(int skillTraxId, CancellationToken cancellationToken = default)
    {
        var skillTrax = await _dbContext.SkillTrax
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SkillTraxId == skillTraxId, cancellationToken);

        if (skillTrax is null)
        {
            return null;
        }

        var courseIds = await _dbContext.SkillTraxCourses
            .AsNoTracking()
            .Where(c => c.SkillTraxId == skillTraxId)
            .Select(c => c.CourseId)
            .ToListAsync(cancellationToken);

        var courses = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => courseIds.Contains(c.CourseId))
            .Select(c => new SkillTraxCourseDto(c.CourseId, c.CourseTitle, c.Duration))
            .ToListAsync(cancellationToken);

        var creator = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == skillTrax.CreatedByUserId)
            .Select(u => new { u.Email, u.FirstName, u.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        var updater = skillTrax.UpdatedByUserId.HasValue
            ? await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.UserId == skillTrax.UpdatedByUserId.Value)
                .Select(u => new { u.Email, u.FirstName, u.LastName })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var companyName = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.CompanyId == skillTrax.CompanyId)
            .Select(c => c.CompanyName)
            .FirstOrDefaultAsync(cancellationToken);

        var assignmentRows = await _dbContext.Assignments
            .AsNoTracking()
            .Where(a => a.SourceSkillTraxId == skillTraxId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new { a.AssignmentId, a.IsCancelled, a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var assignmentIds = assignmentRows.Select(a => a.AssignmentId).ToList();
        var employeeNamesByAssignment = new Dictionary<int, List<string>>();
        if (assignmentIds.Count > 0)
        {
            var employeeRows = await (
                from ae in _dbContext.AssignmentEmployees.AsNoTracking()
                join u in _dbContext.Users.AsNoTracking() on ae.StudentUserId equals u.UserId
                where assignmentIds.Contains(ae.AssignmentId)
                select new { ae.AssignmentId, Name = (u.FirstName ?? "") + " " + (u.LastName ?? "") })
                .ToListAsync(cancellationToken);

            employeeNamesByAssignment = employeeRows
                .GroupBy(x => x.AssignmentId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name.Trim()).ToList());
        }

        var assignments = assignmentRows
            .Select(a => new SkillTraxAssignmentUsageDto(
                a.AssignmentId, a.IsCancelled, a.StartDate, a.EndDate,
                employeeNamesByAssignment.GetValueOrDefault(a.AssignmentId, [])))
            .ToList();

        return new SkillTraxDto(
            skillTrax.SkillTraxId, skillTrax.Name, skillTrax.CompanyId, companyName, skillTrax.CreatedByUserId,
            creator is null ? null : $"{creator.FirstName} {creator.LastName}".Trim(), creator?.Email, skillTrax.CreatedAt,
            updater is null ? null : $"{updater.FirstName} {updater.LastName}".Trim(), updater?.Email, skillTrax.UpdatedAt,
            courses, assignments);
    }
}
