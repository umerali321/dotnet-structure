using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.DTOs;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Students.Interfaces;

public interface IStudentQueryService
{
    Task<PaginatedList<StudentListItemDto>> ListAsync(StudentListQueryOptions options, CancellationToken cancellationToken = default);

    Task<StudentDetailDto?> GetDetailAsync(int userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// RestrictToCompanyIds is the caller's *authorization* boundary (null for SuperAdmin = unrestricted;
/// a Manager's managed companies otherwise) - always computed server-side, never trust a company id
/// supplied by the client beyond validating it falls within this set.
///
/// RestrictToManagerId is set only for a Manager-role caller (their own UserId): once a student's
/// ManagerId is explicitly assigned, only that Manager sees them - RestrictToCompanyIds still governs
/// every unassigned student (ManagerId == null), so nothing changes for existing data.
/// </summary>
public record StudentListQueryOptions(
    int Page,
    int PageSize,
    /// <summary>Which single field to search, and for what. Null means no search - never
    /// "search every column", which is what made this screen take seconds. See SearchCriteria.</summary>
    SearchCriteria? Search,
    string? StudentType,
    bool? IsActive,
    string? SortBy,
    bool SortDescending,
    IReadOnlyCollection<int>? RestrictToCompanyIds,
    int? RestrictToManagerId = null);
