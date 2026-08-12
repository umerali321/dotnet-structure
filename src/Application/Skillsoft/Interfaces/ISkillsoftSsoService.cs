using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.Skillsoft.Interfaces;

public interface ISkillsoftSsoService
{
    Task<string> CreateLaunchTicketAsync(CallerContext caller, int companyId, CancellationToken cancellationToken = default);

    Task<SkillsoftLaunchResult> ConsumeLaunchTicketAsync(string ticket, CancellationToken cancellationToken = default);

    /// <summary>Returns an already-authenticated Skillsoft URL that launches the given course directly (via OLSA's SignOn service), so the caller never sees a Skillsoft login prompt.</summary>
    Task<string> GetCourseLaunchUrlAsync(CallerContext caller, int companyId, string assetId, CancellationToken cancellationToken = default);
}

public record SkillsoftLaunchResult(string RedirectUrl);
