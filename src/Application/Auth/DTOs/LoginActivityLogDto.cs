namespace SkillsetsBackend.Application.Auth.DTOs;

public record LoginActivityLogDto(
    int LoginActivityLogId,
    string EventType,
    string Email,
    int? UserId,
    string? Name,
    string? Phone,
    string? CompanyName,
    string? Message,
    DateTimeOffset CreatedAt);

public record LoginActivitySummaryDto(
    int PasswordResetSucceededCount,
    int PasswordResetFailedCount,
    int SupportRequestSubmittedCount);
