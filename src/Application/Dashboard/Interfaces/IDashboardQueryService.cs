using SkillsetsBackend.Application.Dashboard.Dtos;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Dashboard.Interfaces;

/// <summary>
/// companyIds: null means "no company restriction" (SuperAdmin viewing all companies); a non-null
/// collection restricts to exactly those companies (SuperAdmin picked one, or a CompanyAdmin was
/// force-scoped to the company/companies they manage - an empty collection is valid and simply
/// yields all-zero results, no need to special-case it).
/// </summary>
public interface IDashboardQueryService
{
    Task<DashboardStatsDto> GetStatsAsync(
        IReadOnlyCollection<int>? companyIds,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken);

    /// <summary>restrictToManagerId: when set (a Manager caller), narrows the company-scoped result
    /// further to just that manager's own record plus employees assigned to them (or unassigned -
    /// same "falls through to visible" rule as StudentAuthorization) - mirrors
    /// ListStudentsQueryHandler's identical restrictToManagerId parameter.</summary>
    Task<PaginatedList<CourseLibraryUserDto>> GetCourseLibraryUsersAsync(
        IReadOnlyCollection<int>? companyIds,
        DateOnly? startDate,
        DateOnly? endDate,
        string? search,
        int page,
        int pageSize,
        int? restrictToManagerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseLibrarySessionDto>> GetSessionHistoryAsync(
        string email,
        IReadOnlyCollection<int>? companyIds,
        int? restrictToManagerId,
        CancellationToken cancellationToken);
}
