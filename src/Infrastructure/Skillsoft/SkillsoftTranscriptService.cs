using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Skillsoft.DTOs;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Infrastructure.Options;
using SkillsetsBackend.Infrastructure.Skillsoft.Olsa;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public class SkillsoftTranscriptService : ISkillsoftTranscriptService
{
    private readonly SkillsoftAccessGuard _accessGuard;
    private readonly OlsaSoapClient _olsaClient;
    private readonly SkillsoftOlsaSettings _olsaSettings;

    public SkillsoftTranscriptService(
        SkillsoftAccessGuard accessGuard,
        OlsaSoapClient olsaClient,
        IOptions<SkillsoftOlsaSettings> olsaSettings)
    {
        _accessGuard = accessGuard;
        _olsaClient = olsaClient;
        _olsaSettings = olsaSettings.Value;
    }

    public async Task<PaginatedList<SkillsoftTranscriptEntryDto>> GetTranscriptAsync(
        CallerContext caller, int companyId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var card = await _accessGuard.ResolveForCallerAsync(caller, companyId, cancellationToken);

        // UD_GetAssetResults returns the whole per-user list in one synchronous call - OLSA doesn't
        // paginate this operation itself, so pagination happens here. Safe to do in memory (unlike
        // the 145K-row Students table) because this list is scoped to a single student's own history.
        var results = await _olsaClient.GetAssetResultsAsync(
            _olsaSettings.CustomerId, card.UserId, assetId: null, summaryLevel: true, cancellationToken);

        var pageItems = results
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToList();

        return new PaginatedList<SkillsoftTranscriptEntryDto>(pageItems, results.Count, page, pageSize);
    }

    private static SkillsoftTranscriptEntryDto ToDto(OlsaUsageResult result) =>
        new(result.AssetId, result.Title, result.CompletionStatus, result.FirstAccessDate, result.LastAccessDate, result.CompletionDate, result.Score);
}
