namespace SkillsetsBackend.Application.Faqs.Queries.ListFaqs;

public record ListFaqsQuery(
    int Page,
    int PageSize,
    string? Search,
    int? CompanyId,
    bool? IsActive);
