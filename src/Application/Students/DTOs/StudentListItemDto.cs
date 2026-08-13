namespace SkillsetsBackend.Application.Students.DTOs;

public record StudentListItemDto(
    int UserId,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Username,
    string? Phone,
    string? StudentType,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<StudentCompanyRoleDto> Companies,
    bool HasSkillportAccount);
