using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Assignments;
using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.CourseLibrary;

public class CourseTakenRepository : ICourseTakenRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CourseTakenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CourseTaken?> FindByUserAndCourseAsync(int userId, long courseId, CancellationToken cancellationToken = default) =>
        _dbContext.CourseTakens
            .Where(x => x.UserId == userId && x.CourseId == courseId)
            .OrderByDescending(x => x.CourseTakenId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<CourseTaken?> FindActiveByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.CourseTakens.FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);

    public Task<CourseTaken?> GetByIdAsync(int courseTakenId, CancellationToken cancellationToken = default) =>
        _dbContext.CourseTakens.FirstOrDefaultAsync(x => x.CourseTakenId == courseTakenId, cancellationToken);

    public async Task<bool> TryAddAsync(CourseTaken entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.CourseTakens.AddAsync(entity, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<CourseTakenDto> GetDtoAsync(int courseTakenId, CancellationToken cancellationToken = default)
    {
        // Filter first (on plain entity properties), project to the DTO last - EF Core cannot
        // translate a Where() applied after a Select() that builds a record via string
        // concatenation (student full name), it must come before the final projection.
        var dto = await (
                from ct in _dbContext.CourseTakens.AsNoTracking()
                where ct.CourseTakenId == courseTakenId
                join u in _dbContext.Users.AsNoTracking() on ct.UserId equals u.UserId
                join c in _dbContext.Courses.AsNoTracking() on ct.CourseId equals c.CourseId
                join cat in _dbContext.MainCourseCategories.AsNoTracking() on c.CategoryId equals cat.CategoryId
                select ToDto(ct, u, c, cat))
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            throw new InvalidOperationException($"CourseTaken {courseTakenId} was just written but could not be re-read.");
        }

        var paceLookup = await GetAssignmentStartDatesAsync([(dto.UserId, dto.CourseId)], cancellationToken);
        return WithPace(dto, paceLookup);
    }

    public async Task<PaginatedList<CourseTakenDto>> ListAsync(CourseTakenListOptions options, CancellationToken cancellationToken = default)
    {
        var filtered = _dbContext.CourseTakens.AsNoTracking().AsQueryable();

        if (options.OnlyUserId is not null)
        {
            filtered = filtered.Where(x => x.UserId == options.OnlyUserId.Value);
        }
        else if (options.RestrictToCompanyIds is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            // Deliberately NOT filtered by ucr.Company.IsActive - a company going inactive blocks
            // its users from logging in (enforced separately in
            // UserDirectory.QueryActiveCompanyRoles, which backs login/company-selection), it must
            // not also hide their course-taken records from admin listings/search.
            var studentMemberships = _dbContext.UserCompanyRoles.AsNoTracking().Where(ucr =>
                ucr.IsActive
                && ucr.Role.RoleName == Roles.Student
                && (ucr.StartDate == null || ucr.StartDate <= today)
                && (ucr.EndDate == null || ucr.EndDate >= today));

            var allowed = options.RestrictToCompanyIds;
            filtered = filtered.Where(x => studentMemberships.Any(m => m.UserId == x.UserId && allowed.Contains(m.CompanyId)));
        }
        // else: SuperAdmin - unrestricted.

        if (!string.IsNullOrWhiteSpace(options.StudentNameSearch))
        {
            var term = $"%{EscapeLike(options.StudentNameSearch.Trim())}%";
            filtered = filtered.Where(x => _dbContext.Users.Any(u => u.UserId == x.UserId && (
                EF.Functions.Like(u.FirstName!, term, "\\")
                || EF.Functions.Like(u.LastName!, term, "\\")
                || EF.Functions.Like((u.FirstName ?? "") + " " + (u.LastName ?? ""), term, "\\")
                || EF.Functions.Like((u.LastName ?? "") + " " + (u.FirstName ?? ""), term, "\\"))));
        }

        if (!string.IsNullOrWhiteSpace(options.CourseTitleSearch))
        {
            var term = $"%{EscapeLike(options.CourseTitleSearch.Trim())}%";
            filtered = filtered.Where(x => _dbContext.Courses.Any(c => c.CourseId == x.CourseId && EF.Functions.Like(c.CourseTitle, term, "\\")));
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await (
                from ct in filtered
                orderby ct.AccessedAt descending
                join u in _dbContext.Users.AsNoTracking() on ct.UserId equals u.UserId
                join c in _dbContext.Courses.AsNoTracking() on ct.CourseId equals c.CourseId
                join cat in _dbContext.MainCourseCategories.AsNoTracking() on c.CategoryId equals cat.CategoryId
                select ToDto(ct, u, c, cat))
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync(cancellationToken);

        var paceLookup = await GetAssignmentStartDatesAsync(items.Select(i => (i.UserId, i.CourseId)), cancellationToken);
        var withPace = items.Select(i => WithPace(i, paceLookup)).ToList();

        return new PaginatedList<CourseTakenDto>(withPace, totalCount, options.Page, options.PageSize);
    }

    private static CourseTakenDto ToDto(CourseTaken ct, AppUser u, Course c, MainCourseCategory cat) => new(
        ct.CourseTakenId,
        ct.UserId,
        (((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim()),
        u.Email,
        ct.CourseId,
        c.CourseTitle,
        cat.CategoryName,
        ct.IsActive,
        ct.AccessedAt,
        ct.CompletedAt,
        c.CourseUrl);

    private static CourseTakenDto WithPace(CourseTakenDto dto, IReadOnlyDictionary<(int UserId, long CourseId), DateOnly> assignmentStartByPair) =>
        assignmentStartByPair.TryGetValue((dto.UserId, dto.CourseId), out var assignmentStart)
            ? dto with
            {
                CourseDate = assignmentStart,
                Status = AssignmentTiming.Derive(DateOnly.FromDateTime(dto.AccessedAt.UtcDateTime), assignmentStart).ToString(),
            }
            : dto;

    /// <summary>For each (UserId, CourseId) pair, the StartDate of the most recently created
    /// non-cancelled Assignment that targeted that employee with that course as one of its titles -
    /// or no entry at all if the course was never part of any assignment for them. "Most recently
    /// created" wins when a student was assigned the same course more than once (e.g. a retake
    /// assignment), matching how a fresh assignment is meant to supersede an older one.</summary>
    private async Task<Dictionary<(int UserId, long CourseId), DateOnly>> GetAssignmentStartDatesAsync(
        IEnumerable<(int UserId, long CourseId)> pairs, CancellationToken cancellationToken)
    {
        var pairList = pairs.Distinct().ToList();
        if (pairList.Count == 0)
        {
            return [];
        }

        var userIds = pairList.Select(p => p.UserId).Distinct().ToList();
        var courseIds = pairList.Select(p => p.CourseId).Distinct().ToList();

        var rows = await (
                from ae in _dbContext.AssignmentEmployees.AsNoTracking()
                join at in _dbContext.AssignmentTitles.AsNoTracking() on ae.AssignmentId equals at.AssignmentId
                join a in _dbContext.Assignments.AsNoTracking() on ae.AssignmentId equals a.AssignmentId
                where userIds.Contains(ae.StudentUserId) && courseIds.Contains(at.CourseId) && !a.IsCancelled
                select new { ae.StudentUserId, at.CourseId, a.StartDate, a.CreatedAt })
            .ToListAsync(cancellationToken);

        var pairSet = pairList.ToHashSet();

        return rows
            .Where(r => pairSet.Contains((r.StudentUserId, r.CourseId)))
            .GroupBy(r => (r.StudentUserId, r.CourseId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).First().StartDate);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
