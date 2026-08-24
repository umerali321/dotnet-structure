using SkillsetsBackend.Application.Assignments.DTOs;

namespace SkillsetsBackend.Application.Assignments.Interfaces;

public interface IAssignmentRepository
{
    Task<Domain.Assignments.Assignment?> GetByIdAsync(int assignmentId, CancellationToken cancellationToken = default);

    /// <summary>One batched query across every (employee, course) pair being proposed - finds any
    /// that already has a non-cancelled assignment whose window hasn't ended yet. Returns one row
    /// per overlapping pair, never one query per pair.</summary>
    Task<IReadOnlyList<AssignmentOverlapDto>> FindActiveOverlapsAsync(
        IReadOnlyCollection<int> studentUserIds, IReadOnlyCollection<long> courseIds, DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>Creates the Assignment plus its AssignmentEmployees and AssignmentTitles rows in
    /// one transaction.</summary>
    Task<int> CreateAsync(
        Domain.Assignments.Assignment assignment,
        IReadOnlyList<int> studentUserIds,
        IReadOnlyList<long> courseIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetEmployeeIdsAsync(int assignmentId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the assignment's full AssignmentEmployees set to match employeeUserIds -
    /// removed employees lose access to titles under this assignment; the AssignmentTitles snapshot
    /// itself is untouched, so anyone still on the list keeps exactly the same titles.</summary>
    Task UpdateEmployeesAsync(int assignmentId, IReadOnlyList<int> employeeUserIds, CancellationToken cancellationToken = default);

    /// <summary>Replaces the assignment's full AssignmentTitles set. Caller (UpdateAssignmentCommandHandler)
    /// is responsible for confirming no employee has any progress on the current titles first -
    /// this method itself has no such guard.</summary>
    Task UpdateTitlesAsync(int assignmentId, IReadOnlyList<long> courseIds, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
