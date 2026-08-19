namespace SkillsetsBackend.Application.Companies.Queries.ListCompanies;

/// <summary>StatusFilter is one of "Trial", "Licensed", "Expired", "Deactivated", or null for no filter -
/// see CompanyQueryService/sp_ListCompanies for the exact bucket definitions.</summary>
public record ListCompaniesQuery(string? Search, bool IncludeInactive = false, int Page = 1, int PageSize = 100, string? StatusFilter = null);
