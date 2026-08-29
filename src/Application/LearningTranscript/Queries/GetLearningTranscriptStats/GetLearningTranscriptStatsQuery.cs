namespace SkillsetsBackend.Application.LearningTranscript.Queries.GetLearningTranscriptStats;

public record GetLearningTranscriptStatsQuery(int? CompanyId, DateOnly? DateFrom, DateOnly? DateTo);
