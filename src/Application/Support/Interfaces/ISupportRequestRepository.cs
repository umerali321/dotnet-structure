using SkillsetsBackend.Application.Support.DTOs;
using SkillsetsBackend.Domain.Support;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Support.Interfaces;

public record SupportRequestListQueryOptions(
    int Page,
    int PageSize,
    int? CompanyId,
    string? Status,
    IReadOnlyCollection<int>? RestrictToCompanyIds,
    int? RestrictToUserId);

public interface ISupportRequestRepository
{
    Task<PaginatedList<SupportRequestDto>> ListAsync(SupportRequestListQueryOptions options, CancellationToken cancellationToken = default);

    Task<SupportRequestDto?> GetDtoAsync(int supportRequestId, CancellationToken cancellationToken = default);

    Task<SupportRequest?> GetEntityAsync(int supportRequestId, CancellationToken cancellationToken = default);

    Task AddAsync(SupportRequest request, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
