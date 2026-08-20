namespace SkillsetsBackend.Application.Dashboard.Queries.GetDashboardStats;

public record GetDashboardStatsQuery(int? CompanyId, DateOnly? StartDate, DateOnly? EndDate);
