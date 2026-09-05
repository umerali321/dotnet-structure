using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Assignments.Interfaces;

public interface IAssignmentQueryService
{
    /// <summary>Manager/CompanyAdmin "Ongoing Assignments" view. Null companyIds means unrestricted
    /// (SuperAdmin only) - an empty collection means "restricted to zero companies." trainingName
    /// matches the linked SkillTrax's name or any of the assignment's title course names;
    /// employeeName matches any targeted employee's first/last name.</summary>
    Task<PaginatedList<AssignmentDto>> ListManagedAsync(
        IReadOnlyCollection<int>? companyIds, int page, int pageSize,
        string? trainingName = null, string? employeeName = null, CancellationToken cancellationToken = default);

    /// <summary>Employee "My Assignments" view - every non-cancelled assignment that targets this
    /// one employee, most recent first. Not paginated - a single employee's own assignment count
    /// is never large enough to need it.</summary>
    Task<IReadOnlyList<AssignmentDto>> ListMineAsync(int studentUserId, CancellationToken cancellationToken = default);

    Task<AssignmentDto?> GetDtoAsync(int assignmentId, CancellationToken cancellationToken = default);
}
