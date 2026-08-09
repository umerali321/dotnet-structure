namespace SkillsetsBackend.Application.SupportContacts.Queries.ListSupportContacts;

public record ListSupportContactsQuery(
    int Page,
    int PageSize,
    int? CompanyId,
    bool? IsActive);
