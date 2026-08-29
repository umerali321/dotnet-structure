namespace SkillsetsBackend.Application.LearningTranscript.DTOs;

public record LearningTranscriptImportResultDto(
    int ImportBatchId,
    int TotalRows,
    int MatchedCount,
    int UnmatchedCount,
    string? UnmatchedGroupOrgCodes);
