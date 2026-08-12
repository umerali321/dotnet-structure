using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Skillsoft.DTOs;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Skillsoft.Interfaces;

public interface ISkillsoftCatalogService
{
    /// <summary>Searches the caller's entitled Skillsoft catalog. Backed by OLSA's Search &amp; Learn service (SL_FederatedSearch + SL_PaginateSearch).</summary>
    Task<PaginatedList<SkillsoftAssetSummaryDto>> SearchAsync(
        CallerContext caller, int companyId, string searchPhrase, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<SkillsoftAssetDetailDto> GetAssetDetailAsync(
        CallerContext caller, int companyId, string assetId, CancellationToken cancellationToken = default);
}
