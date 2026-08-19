namespace SkillsetsBackend.Application.Companies.Commands.CreateCompany;

/// <summary>Creates a new company together with its first CompanyAdmin user, who can then create
/// Managers for this company. PlanType is Company.TrialPlan ("Trial") or Company.LicensePlan
/// ("License") - for a License, LicenseStartDate/LicenseEndDate are required.</summary>
public record CreateCompanyCommand(
    string CompanyCode,
    string CompanyName,
    string? CompanyEmail,
    string? CompanyPhone,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminUsername,
    string AdminPassword,
    string PlanType,
    DateOnly? LicenseStartDate,
    DateOnly? LicenseEndDate,
    string? Street1 = null,
    string? Street2 = null,
    string? City = null,
    string? State = null,
    string? Zip = null,
    string? PaymentForm = null,
    decimal? TotalPayment = null);
