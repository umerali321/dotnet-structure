namespace SkillsetsBackend.Application.Settings.Queries.ListEmailHistory;

public record ListEmailHistoryQuery(int Page, int PageSize, string? Search = null);
