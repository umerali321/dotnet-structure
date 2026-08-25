using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Companies.DTOs;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Companies;

public class CompanyQueryService : ICompanyQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public CompanyQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Backed by dbo.sp_ListCompanies (see Persistence/Migrations - CreateListCompaniesStoredProcedure)
    /// rather than an inline LINQ query, per team convention: paging/search/expiry-status logic for
    /// this list lives in one stored procedure instead of being reimplemented in C#.</summary>
    public async Task<PaginatedList<CompanyListItemDto>> ListAsync(
        IReadOnlyCollection<int>? restrictToCompanyIds,
        string? search,
        bool includeInactive,
        string? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Distinguish "no restriction" (null) from "restricted to zero companies" (empty collection) -
        // the latter must return nothing without a round-trip, since STRING_SPLIT('', ',') does not
        // reliably mean "empty set" across SQL Server versions.
        if (restrictToCompanyIds is { Count: 0 })
        {
            return new PaginatedList<CompanyListItemDto>([], 0, page, pageSize);
        }

        var restrictParam = restrictToCompanyIds is null ? null : string.Join(",", restrictToCompanyIds);
        // "Licensed" (the UI label) maps onto Company.PlanType's actual stored value, "License".
        var statusFilterParam = statusFilter == "Licensed" ? "License" : statusFilter;

        var rows = await _dbContext.Database
            .SqlQuery<CompanyRow>(
                $"EXEC dbo.sp_ListCompanies @Search={search}, @IncludeInactive={includeInactive}, @RestrictToCompanyIds={restrictParam}, @StatusFilter={statusFilterParam}, @Page={page}, @PageSize={pageSize}")
            .ToListAsync(cancellationToken);

        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        var items = rows
            .Select(r => new CompanyListItemDto(
                r.CompanyId, r.CompanyCode, r.CompanyName, r.IsActive, r.LogoUrl, r.PlanType, r.PlanStartDate, r.PlanEndDate, r.IsExpired,
                r.CompanyEmail, r.CompanyPhone, r.Street1, r.Street2, r.City, r.State, r.Zip, r.PaymentForm, r.TotalPayment, r.PurchaseDate))
            .ToList();

        return new PaginatedList<CompanyListItemDto>(items, totalCount, page, pageSize);
    }

    private sealed record CompanyRow(
        int CompanyId, string CompanyCode, string CompanyName, bool IsActive, string? LogoUrl,
        string PlanType, DateOnly PlanStartDate, DateOnly PlanEndDate, bool IsExpired,
        string? CompanyEmail, string? CompanyPhone, string? Street1, string? Street2, string? City,
        string? State, string? Zip, string? PaymentForm, decimal? TotalPayment, DateOnly? PurchaseDate, int TotalCount);
}
