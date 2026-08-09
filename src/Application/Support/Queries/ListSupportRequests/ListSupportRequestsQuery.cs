namespace SkillsetsBackend.Application.Support.Queries.ListSupportRequests;

public record ListSupportRequestsQuery(int Page, int PageSize, int? CompanyId, string? Status);
