namespace SkillsetsBackend.Application.Skillsoft.DTOs;

public record SkillsoftTranscriptEntryDto(
    string? AssetId,
    string? Title,
    string? CompletionStatus,
    string? FirstAccessDate,
    string? LastAccessDate,
    string? CompletionDate,
    string? Score);
