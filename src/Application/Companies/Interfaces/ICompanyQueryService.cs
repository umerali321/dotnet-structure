using SkillsetsBackend.Application.Companies.DTOs;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Companies.Interfaces;

public interface ICompanyQueryService
{
    /// <summary>Backed by the dbo.sp_ListCompanies stored procedure - see CompanyQueryService.
    /// statusFilter is one of "Trial", "Licensed", "Expired", "Deactivated", or null.</summary>
    Task<PaginatedList<CompanyListItemDto>> ListAsync(
        IReadOnlyCollection<int>? restrictToCompanyIds,
        string? search,
        bool includeInactive,
        string? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
