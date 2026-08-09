namespace SkillsetsBackend.Application.Faqs.DTOs;

public record FaqDto(
    int FaqId,
    int? CompanyId,
    string? CompanyName,
    string Question,
    string Answer,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy);
