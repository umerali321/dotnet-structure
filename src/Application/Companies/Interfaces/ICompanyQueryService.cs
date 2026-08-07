using SkillsetsBackend.Application.Companies.DTOs;

namespace SkillsetsBackend.Application.Companies.Interfaces;

public interface ICompanyQueryService
{
    Task<IReadOnlyList<CompanyListItemDto>> ListAsync(
        IReadOnlyCollection<int>? restrictToCompanyIds,
        string? search,
        CancellationToken cancellationToken = default);
}
