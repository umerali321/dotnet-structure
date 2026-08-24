namespace SkillsetsBackend.Application.Assignments.Queries.ListOngoingAssignments;

public record ListOngoingAssignmentsQuery(int Page, int PageSize, int? CompanyId);
