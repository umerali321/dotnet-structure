using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Domain.Communications;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Settings.Interfaces;

public interface IEmailLogRepository
{
    /// <summary>Best-effort - called from the email-sending infrastructure right after every send
    /// attempt (success or failure). A failure to write this log must never fail the caller's actual
    /// email send/request, so implementations should swallow their own errors internally.</summary>
    Task AddAsync(EmailLog log, CancellationToken cancellationToken = default);

    /// <summary>search, when provided, filters to rows whose ToAddress contains the term
    /// (case-insensitive). purpose, when provided, filters to that exact Purpose value.</summary>
    Task<PaginatedList<EmailLogDto>> ListAsync(
        int page, int pageSize, string? search, string? purpose, CancellationToken cancellationToken = default);

    Task<EmailLogDetailDto?> GetByIdAsync(int emailLogId, CancellationToken cancellationToken = default);
}
