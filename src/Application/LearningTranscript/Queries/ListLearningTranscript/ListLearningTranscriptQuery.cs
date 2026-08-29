namespace SkillsetsBackend.Application.LearningTranscript.Queries.ListLearningTranscript;

public record ListLearningTranscriptQuery(
    int Page,
    int PageSize,
    string? Search,
    int? CompanyId,
    string? AssetId,
    string? CompletionStatus,
    DateOnly? DateFrom,
    DateOnly? DateTo);
