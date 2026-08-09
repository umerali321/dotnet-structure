namespace SkillsetsBackend.Application.SupportContacts.DTOs;

public record SupportContactDto(
    int SupportContactId,
    int? CompanyId,
    string? CompanyName,
    string ContactType,
    string Value,
    int SortOrder,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy);
