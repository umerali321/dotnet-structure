namespace SkillsetsBackend.Application.Managers.DTOs;
public record ManagerListItemDto(int UserId,string? FirstName,string? LastName,string? Email,string? Username,string? Phone,bool IsActive,DateTimeOffset CreatedAt,IReadOnlyList<ManagerCompanyDto> Companies,bool HasSkillportAccount);
public record ManagerCompanyDto(int CompanyId,string CompanyCode,string CompanyName,string Role,DateOnly? StartDate,DateOnly? EndDate);
