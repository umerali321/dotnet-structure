using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.SkillTrax.Interfaces;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Assignments;

public class SkillTraxRepository : ISkillTraxRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SkillTraxRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Domain.Assignments.SkillTrax?> GetByIdAsync(int skillTraxId, CancellationToken cancellationToken = default) =>
        _dbContext.SkillTrax.FirstOrDefaultAsync(x => x.SkillTraxId == skillTraxId, cancellationToken);

    public async Task<int> CreateAsync(Domain.Assignments.SkillTrax skillTrax, IReadOnlyList<long> courseIds, CancellationToken cancellationToken = default)
    {
        // EnableRetryOnFailure requires operations wrapped this way - see StudentRepository.CreateStudentAsync
        // for the identical precedent.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            _dbContext.SkillTrax.Add(skillTrax);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var courseId in courseIds)
            {
                _dbContext.SkillTraxCourses.Add(new SkillTraxCourse(skillTrax.SkillTraxId, courseId));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return skillTrax.SkillTraxId;
        });
    }

    public async Task UpdateAsync(Domain.Assignments.SkillTrax skillTrax, IReadOnlyList<long> courseIds, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            _dbContext.SkillTrax.Update(skillTrax);

            var existing = await _dbContext.SkillTraxCourses
                .Where(c => c.SkillTraxId == skillTrax.SkillTraxId)
                .ToListAsync(cancellationToken);
            _dbContext.SkillTraxCourses.RemoveRange(existing);

            foreach (var courseId in courseIds)
            {
                _dbContext.SkillTraxCourses.Add(new SkillTraxCourse(skillTrax.SkillTraxId, courseId));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public Task<bool> HasActiveAssignmentAsync(int skillTraxId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return _dbContext.Assignments.AnyAsync(
            a => a.SourceSkillTraxId == skillTraxId && !a.IsCancelled && a.EndDate >= today, cancellationToken);
    }

    public async Task DeleteAsync(Domain.Assignments.SkillTrax skillTrax, CancellationToken cancellationToken = default)
    {
        // Soft delete - the row (and its SkillTraxCourses) stay in the database for audit/history;
        // SkillTrax's EF global query filter (IsDeleted) keeps it out of every normal list/detail/
        // get-by-id query from here on.
        skillTrax.Delete();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
