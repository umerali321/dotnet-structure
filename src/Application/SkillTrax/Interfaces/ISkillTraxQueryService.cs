using SkillsetsBackend.Application.SkillTrax.DTOs;

namespace SkillsetsBackend.Application.SkillTrax.Interfaces;

public interface ISkillTraxQueryService
{
    /// <summary>Null companyIds means unrestricted (SuperAdmin only) - an empty collection means
    /// "restricted to zero companies," which correctly yields zero results, not everything.</summary>
    Task<IReadOnlyList<SkillTraxSummaryDto>> ListAsync(
        IReadOnlyCollection<int>? companyIds, CancellationToken cancellationToken = default);

    Task<SkillTraxDto?> GetDetailAsync(int skillTraxId, CancellationToken cancellationToken = default);
}
