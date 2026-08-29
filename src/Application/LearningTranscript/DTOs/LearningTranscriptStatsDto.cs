namespace SkillsetsBackend.Application.LearningTranscript.DTOs;

public record LearningTranscriptStatsDto(
    int PeopleWithActivity,
    int DistinctCoursesTaken,
    int TotalCompletions,
    int TotalInProgress,
    int TotalActivityRows,
    decimal CompletionRatePercent,
    decimal AvgSessionsPerEmployee);
