using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface ILoginActivityLogRepository
{
    Task AddAsync(LoginActivityLog log, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>search matches against Email OR Name.</summary>
    Task<PaginatedList<LoginActivityLogDto>> ListAsync(
        int page,
        int pageSize,
        string? eventType,
        string? search,
        string? companyName,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken = default);

    Task<LoginActivitySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>Timestamps of up to <paramref name="maxCount"/> most recent LoginFailed events for
    /// this email since <paramref name="sinceUtc"/>, newest first - used to drive login lockout.</summary>
    Task<IReadOnlyList<DateTimeOffset>> GetRecentFailedLoginTimestampsAsync(
        string email, DateTimeOffset sinceUtc, int maxCount, CancellationToken cancellationToken = default);
}
