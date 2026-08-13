namespace SkillsetsBackend.Application.Companies.Commands.CreateCompany;

/// <summary>Creates a new company together with its first CompanyAdmin user, who can then create
/// Managers for this company.</summary>
public record CreateCompanyCommand(
    string CompanyCode,
    string CompanyName,
    string? CompanyEmail,
    string? CompanyPhone,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminUsername,
    string AdminPassword);
