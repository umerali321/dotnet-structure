using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface ILoginActivityLogRepository
{
    Task AddAsync(LoginActivityLog log, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<PaginatedList<LoginActivityLogDto>> ListAsync(int page, int pageSize, string? eventType, CancellationToken cancellationToken = default);

    Task<LoginActivitySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
