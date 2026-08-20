namespace SkillsetsBackend.Application.Auth.Queries.ListLoginActivityLogs;

public record ListLoginActivityLogsQuery(
    int Page,
    int PageSize,
    string? EventType,
    string? Email,
    string? Name,
    string? CompanyName,
    DateOnly? StartDate,
    DateOnly? EndDate);
