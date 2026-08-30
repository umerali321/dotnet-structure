namespace SkillsetsBackend.Application.Settings.Queries.ListEmailHistory;

/// <param name="Purpose">Exact Purpose value to filter to ("PasswordReset", "AssignmentCreated",
/// ...), or null for every purpose.</param>
public record ListEmailHistoryQuery(int Page, int PageSize, string? Search = null, string? Purpose = null);
