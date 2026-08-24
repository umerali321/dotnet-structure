namespace SkillsetsBackend.Application.SkillTrax.Interfaces;

public interface ISkillTraxRepository
{
    Task<Domain.Assignments.SkillTrax?> GetByIdAsync(int skillTraxId, CancellationToken cancellationToken = default);

    /// <summary>Creates the SkillTrax and its SkillTraxCourses membership rows in one transaction.</summary>
    Task<int> CreateAsync(Domain.Assignments.SkillTrax skillTrax, IReadOnlyList<long> courseIds, CancellationToken cancellationToken = default);

    /// <summary>Renames and replaces the full SkillTraxCourses set in one transaction. Safe
    /// unconditionally - see SkillTrax's own doc comment for why this never touches historical
    /// assignment data.</summary>
    Task UpdateAsync(Domain.Assignments.SkillTrax skillTrax, IReadOnlyList<long> courseIds, CancellationToken cancellationToken = default);

    /// <summary>True if a non-cancelled assignment (EndDate in the future or today) still
    /// references this SkillTrax - blocks deletion per the blueprint's "don't delete while an
    /// active assignment depends on it" rule.</summary>
    Task<bool> HasActiveAssignmentAsync(int skillTraxId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Domain.Assignments.SkillTrax skillTrax, CancellationToken cancellationToken = default);
}
