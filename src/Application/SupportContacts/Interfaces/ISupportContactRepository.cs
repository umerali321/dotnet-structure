using SkillsetsBackend.Application.SupportContacts.DTOs;
using SkillsetsBackend.Domain.Support;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.SupportContacts.Interfaces;

public record SupportContactListQueryOptions(
    int Page,
    int PageSize,
    int? CompanyId,
    bool? IsActive,
    IReadOnlyCollection<int>? RestrictToCompanyIds);

public interface ISupportContactRepository
{
    Task<PaginatedList<SupportContactDto>> ListAsync(SupportContactListQueryOptions options, CancellationToken cancellationToken = default);

    Task<SupportContactDto?> GetDtoAsync(int supportContactId, CancellationToken cancellationToken = default);

    Task<SupportContact?> GetEntityAsync(int supportContactId, CancellationToken cancellationToken = default);

    Task AddAsync(SupportContact contact, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
