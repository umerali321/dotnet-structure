namespace SkillsetsBackend.Application.Auth.Queries.ListLoginActivityLogs;

// Search matches against Email OR Name (whichever is populated for that event type).
public record ListLoginActivityLogsQuery(
    int Page,
    int PageSize,
    string? EventType,
    string? Search,
    string? CompanyName,
    DateOnly? StartDate,
    DateOnly? EndDate);
