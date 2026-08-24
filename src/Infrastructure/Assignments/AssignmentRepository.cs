using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Application.Assignments.Interfaces;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Assignments;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AssignmentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Assignment?> GetByIdAsync(int assignmentId, CancellationToken cancellationToken = default) =>
        _dbContext.Assignments.FirstOrDefaultAsync(a => a.AssignmentId == assignmentId, cancellationToken);

    public async Task<IReadOnlyList<AssignmentOverlapDto>> FindActiveOverlapsAsync(
        IReadOnlyCollection<int> studentUserIds, IReadOnlyCollection<long> courseIds, DateOnly today, CancellationToken cancellationToken = default)
    {
        if (studentUserIds.Count == 0 || courseIds.Count == 0)
        {
            return [];
        }

        var pairs = await (
            from ae in _dbContext.AssignmentEmployees.AsNoTracking()
            join a in _dbContext.Assignments.AsNoTracking() on ae.AssignmentId equals a.AssignmentId
            join at in _dbContext.AssignmentTitles.AsNoTracking() on a.AssignmentId equals at.AssignmentId
            where studentUserIds.Contains(ae.StudentUserId)
                  && courseIds.Contains(at.CourseId)
                  && !a.IsCancelled
                  && a.EndDate >= today
            select new { ae.StudentUserId, at.CourseId })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (pairs.Count == 0)
        {
            return [];
        }

        var userIds = pairs.Select(p => p.StudentUserId).Distinct().ToList();
        var userNames = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, Name = (u.FirstName ?? "") + " " + (u.LastName ?? "") })
            .ToDictionaryAsync(u => u.UserId, u => u.Name.Trim(), cancellationToken);

        var pairCourseIds = pairs.Select(p => p.CourseId).Distinct().ToList();
        var courseTitles = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => pairCourseIds.Contains(c.CourseId))
            .ToDictionaryAsync(c => c.CourseId, c => c.CourseTitle, cancellationToken);

        return pairs
            .Select(p => new AssignmentOverlapDto(
                p.StudentUserId,
                string.IsNullOrWhiteSpace(userNames.GetValueOrDefault(p.StudentUserId)) ? "This employee" : userNames[p.StudentUserId],
                p.CourseId,
                courseTitles.GetValueOrDefault(p.CourseId, "This course")))
            .ToList();
    }

    public async Task<int> CreateAsync(
        Assignment assignment, IReadOnlyList<int> studentUserIds, IReadOnlyList<long> courseIds, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            _dbContext.Assignments.Add(assignment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var studentUserId in studentUserIds)
            {
                _dbContext.AssignmentEmployees.Add(new AssignmentEmployee(assignment.AssignmentId, studentUserId));
            }

            foreach (var courseId in courseIds)
            {
                _dbContext.AssignmentTitles.Add(new AssignmentTitle(assignment.AssignmentId, courseId));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return assignment.AssignmentId;
        });
    }

    public async Task<IReadOnlyList<int>> GetEmployeeIdsAsync(int assignmentId, CancellationToken cancellationToken = default) =>
        await _dbContext.AssignmentEmployees
            .AsNoTracking()
            .Where(ae => ae.AssignmentId == assignmentId)
            .Select(ae => ae.StudentUserId)
            .ToListAsync(cancellationToken);

    public async Task UpdateEmployeesAsync(int assignmentId, IReadOnlyList<int> employeeUserIds, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existing = await _dbContext.AssignmentEmployees
                .Where(ae => ae.AssignmentId == assignmentId)
                .ToListAsync(cancellationToken);
            _dbContext.AssignmentEmployees.RemoveRange(existing);

            foreach (var studentUserId in employeeUserIds)
            {
                _dbContext.AssignmentEmployees.Add(new AssignmentEmployee(assignmentId, studentUserId));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task UpdateTitlesAsync(int assignmentId, IReadOnlyList<long> courseIds, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existing = await _dbContext.AssignmentTitles
                .Where(at => at.AssignmentId == assignmentId)
                .ToListAsync(cancellationToken);
            _dbContext.AssignmentTitles.RemoveRange(existing);

            foreach (var courseId in courseIds)
            {
                _dbContext.AssignmentTitles.Add(new AssignmentTitle(assignmentId, courseId));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
}
