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
/// </summary>
public record StudentListQueryOptions(
    int Page,
    int PageSize,
    string? Search,
    string? StudentType,
    bool? IsActive,
    string? SortBy,
    bool SortDescending,
    IReadOnlyCollection<int>? RestrictToCompanyIds);
