namespace SkillsetsBackend.Application.Students.DTOs;

public record StudentCompanyRoleDto(
    int CompanyId,
    string CompanyCode,
    string CompanyName,
    string Role,
    DateOnly? StartDate,
    DateOnly? EndDate);
