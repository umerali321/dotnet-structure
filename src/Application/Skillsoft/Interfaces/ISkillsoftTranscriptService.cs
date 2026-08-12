using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Skillsoft.DTOs;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Skillsoft.Interfaces;

public interface ISkillsoftTranscriptService
{
    /// <summary>Returns the caller's learning transcript (activity/progress/completions). Backed by OLSA's UD_GetAssetResults, paginated server-side since OLSA returns the full per-user list in one call.</summary>
    Task<PaginatedList<SkillsoftTranscriptEntryDto>> GetTranscriptAsync(
        CallerContext caller, int companyId, int page, int pageSize, CancellationToken cancellationToken = default);
}
