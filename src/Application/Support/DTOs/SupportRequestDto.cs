namespace SkillsetsBackend.Application.Support.DTOs;

public record SupportRequestDto(
    int SupportRequestId,
    int CompanyId,
    string? CompanyName,
    int UserId,
    string? UserEmail,
    string Subject,
    string Message,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
