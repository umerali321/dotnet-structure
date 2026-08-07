namespace SkillsetsBackend.Application.Students.DTOs;

public record StudentDetailDto(
    int UserId,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Username,
    string? Phone,
    string? StudentType,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy,
    IReadOnlyList<StudentCompanyRoleDto> Companies);
