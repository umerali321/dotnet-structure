using SkillsetsBackend.Application.LearningTranscript.DTOs;

namespace SkillsetsBackend.Application.LearningTranscript.Interfaces;

public interface ILearningTranscriptImportService
{
    /// <summary>Parses a Skillport "Asset Activity by User" style .xlsx export (the same shape the
    /// skillport-scraper tool produces) and loads it via sp_ImportLearningTranscriptBatch.</summary>
    Task<LearningTranscriptImportResultDto> ImportAsync(
        Stream fileContent,
        string sourceFileName,
        string importedBy,
        CancellationToken cancellationToken = default);
}
