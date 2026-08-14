using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.Skillsoft.Interfaces;

public interface ISkillsoftSsoService
{
    Task<string> CreateLaunchTicketAsync(CallerContext caller, int companyId, CancellationToken cancellationToken = default);

    Task<SkillsoftLaunchResult> ConsumeLaunchTicketAsync(string ticket, CancellationToken cancellationToken = default);

    /// <summary>Returns an already-authenticated Skillsoft URL that launches the given course directly (via OLSA's SignOn service), so the caller never sees a Skillsoft login prompt.</summary>
    Task<string> GetCourseLaunchUrlAsync(CallerContext caller, int companyId, string assetId, CancellationToken cancellationToken = default);

    /// <summary>Whether the caller currently has an active 30-day Skillport session at this company, without throwing if they don't.</summary>
    Task<SkillsoftSessionStatus> GetSessionStatusAsync(CallerContext caller, int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-service session start: if the caller already has an active session, reuses it; otherwise
    /// provisions a brand-new Skillport account for them (their own username, a fresh 30-day window) and
    /// then behaves like CreateLaunchTicketAsync. Throws if provisioning fails - the caller explicitly
    /// asked to start a session, so a failure here should surface, not be silently ignored.
    /// </summary>
    Task<string> StartSessionAsync(CallerContext caller, int companyId, CancellationToken cancellationToken = default);
}

public record SkillsoftLaunchResult(string RedirectUrl);

public record SkillsoftSessionStatus(bool HasActiveSession, bool IsExpired, bool HasDormantAccount, DateTime? StartDate, DateTime? EndDate);
