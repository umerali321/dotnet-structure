using SkillsetsBackend.Application.LearningTranscript.DTOs;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.LearningTranscript.Interfaces;

public interface ILearningTranscriptQueryService
{
    Task<PaginatedList<LearningTranscriptListItemDto>> ListAsync(LearningTranscriptQueryOptions options, CancellationToken cancellationToken = default);

    Task<LearningTranscriptStatsDto> GetStatsAsync(LearningTranscriptQueryOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// RestrictToCompanyIds/RestrictToManagerId/RestrictToUserId are the caller's *authorization*
/// boundary - always computed server-side by the handler from the caller's live roles, exactly
/// like StudentListQueryOptions. RestrictToUserId forces a Student/Employee caller to see only
/// their own transcript regardless of any filter the client sends.
/// </summary>
public record LearningTranscriptQueryOptions(
    int Page,
    int PageSize,
    string? Search,
    string? AssetId,
    string? CompletionStatus,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    IReadOnlyCollection<int>? RestrictToCompanyIds,
    int? RestrictToManagerId,
    int? RestrictToUserId);
