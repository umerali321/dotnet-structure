namespace SkillsetsBackend.Application.Companies.DTOs;

public record CompanyListItemDto(
    int CompanyId,
    string CompanyCode,
    string CompanyName,
    bool IsActive,
    string? LogoUrl,
    string PlanType,
    DateOnly PlanStartDate,
    DateOnly PlanEndDate,
    bool IsExpired);
