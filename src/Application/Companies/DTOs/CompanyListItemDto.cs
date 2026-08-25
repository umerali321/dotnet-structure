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
    bool IsExpired,
    string? CompanyEmail = null,
    string? CompanyPhone = null,
    string? Street1 = null,
    string? Street2 = null,
    string? City = null,
    string? State = null,
    string? Zip = null,
    string? PaymentForm = null,
    decimal? TotalPayment = null,
    DateOnly? PurchaseDate = null);
