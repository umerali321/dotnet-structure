using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Skillsoft.DTOs;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Infrastructure.Options;
using SkillsetsBackend.Infrastructure.Skillsoft.Olsa;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public class SkillsoftCatalogService : ISkillsoftCatalogService
{
    private const string SearchCacheKeyPrefix = "skillsoft-search:";
    private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromMinutes(15);

    private readonly SkillsoftAccessGuard _accessGuard;
    private readonly OlsaSoapClient _olsaClient;
    private readonly SkillsoftOlsaSettings _olsaSettings;
    private readonly IMemoryCache _cache;

    public SkillsoftCatalogService(
        SkillsoftAccessGuard accessGuard,
        OlsaSoapClient olsaClient,
        IOptions<SkillsoftOlsaSettings> olsaSettings,
        IMemoryCache cache)
    {
        _accessGuard = accessGuard;
        _olsaClient = olsaClient;
        _olsaSettings = olsaSettings.Value;
        _cache = cache;
    }

    public async Task<PaginatedList<SkillsoftAssetSummaryDto>> SearchAsync(
        CallerContext caller, int companyId, string searchPhrase, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var card = await _accessGuard.ResolveForCallerAsync(caller, companyId, cancellationToken);
        var cacheKey = $"{SearchCacheKeyPrefix}{caller.DbUserId}:{companyId}:{searchPhrase.Trim().ToLowerInvariant()}";

        List<OlsaAsset> assets;
        bool hasMore;

        if (page == 1)
        {
            var firstPage = await _olsaClient.FederatedSearchAsync(_olsaSettings.CustomerId, searchPhrase, card.UserId, cancellationToken: cancellationToken);
            assets = firstPage.Assets.Take(pageSize).ToList();
            hasMore = firstPage.Assets.Count > pageSize || firstPage.HasMore;

            if (!string.IsNullOrEmpty(firstPage.SearchId))
            {
                var binNames = firstPage.Assets
                    .Select(a => a.BinName)
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Distinct()
                    .ToList();
                _cache.Set(cacheKey, new CachedSearch(firstPage.SearchId, binNames), SearchCacheDuration);
            }
        }
        else if (_cache.TryGetValue(cacheKey, out CachedSearch? cached) && cached is not null)
        {
            // SL_PaginateSearch only pages one bin at a time, so page through every bin the first
            // page touched and merge - see the wire-format caveat in OlsaSoapClient.
            var start = (page - 1) * pageSize;
            var binNames = cached.BinNames.Count > 0 ? cached.BinNames : [null];
            var merged = new List<OlsaAsset>();
            foreach (var binName in binNames)
            {
                var pageResult = await _olsaClient.PaginateSearchAsync(_olsaSettings.CustomerId, cached.SearchId, binName, start, pageSize, cancellationToken);
                merged.AddRange(pageResult.Assets);
            }

            assets = merged.Take(pageSize).ToList();
            hasMore = merged.Count > pageSize;
        }
        else
        {
            // OLSA's searchId is short-lived server-side; a cache miss past page 1 means it expired.
            return await SearchAsync(caller, companyId, searchPhrase, 1, pageSize, cancellationToken);
        }

        // OLSA's bin-based cursor never reports a total count, so this is an estimate: known items
        // so far, plus one more if there's evidence of another page.
        var totalCount = (page - 1) * pageSize + assets.Count + (hasMore ? 1 : 0);
        var dtos = assets.Select(ToDto).ToList();

        return new PaginatedList<SkillsoftAssetSummaryDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<SkillsoftAssetDetailDto> GetAssetDetailAsync(
        CallerContext caller, int companyId, string assetId, CancellationToken cancellationToken = default)
    {
        var card = await _accessGuard.ResolveForCallerAsync(caller, companyId, cancellationToken);
        var asset = await _olsaClient.GetAssetDetailAsync(_olsaSettings.CustomerId, assetId, card.UserId, cancellationToken);

        if (asset is null)
        {
            throw new NotFoundException("Skillsoft asset", assetId);
        }

        return new SkillsoftAssetDetailDto(asset.AssetId, asset.Title, asset.AssetType, asset.LanguageCode, asset.Fields);
    }

    private static SkillsoftAssetSummaryDto ToDto(OlsaAsset asset) =>
        new(asset.AssetId, asset.Title, asset.AssetType, asset.BinName, asset.LanguageCode);

    private sealed record CachedSearch(string SearchId, IReadOnlyList<string?> BinNames);
}
