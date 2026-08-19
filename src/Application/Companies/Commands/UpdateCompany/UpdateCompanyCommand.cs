namespace SkillsetsBackend.Application.Companies.Commands.UpdateCompany;

public record UpdateCompanyCommand(
    string CompanyCode,
    string CompanyName,
    string? CompanyEmail = null,
    string? CompanyPhone = null,
    string? Street1 = null,
    string? Street2 = null,
    string? City = null,
    string? State = null,
    string? Zip = null,
    string? PaymentForm = null,
    decimal? TotalPayment = null);
