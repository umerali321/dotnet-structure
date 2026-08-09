using SkillsetsBackend.Application.Faqs.DTOs;
using SkillsetsBackend.Domain.Support;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Faqs.Interfaces;

public record FaqListQueryOptions(
    int Page,
    int PageSize,
    string? Search,
    int? CompanyId,
    bool? IsActive,
    IReadOnlyCollection<int>? RestrictToCompanyIds);

public interface IFaqRepository
{
    Task<PaginatedList<FaqDto>> ListAsync(FaqListQueryOptions options, CancellationToken cancellationToken = default);

    Task<FaqDto?> GetDtoAsync(int faqId, CancellationToken cancellationToken = default);

    Task<Faq?> GetEntityAsync(int faqId, CancellationToken cancellationToken = default);

    Task AddAsync(Faq faq, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
