namespace SkillsetsBackend.Application.Skillsoft.Interfaces;

/// <summary>
/// Application-layer entry point for the Skillport 30-day session lifecycle (registration-time dormant
/// account creation, admin-triggered "create/retry now"). The self-service activate/expire/restart flow
/// lives behind ISkillsoftSsoService.StartSessionAsync instead, since it's tied to the launch-ticket flow.
/// </summary>
public interface ISkillportSessionService
{
    /// <summary>Best-effort: creates the user's Skillport account now but leaves the session dormant (no dates) until they first enter the course library. Never throws for a Skillport rejection.</summary>
    Task<SkillsoftProvisionResult> EnsureDormantAccountAsync(int userId, int companyId, CancellationToken cancellationToken = default);

    /// <summary>Admin-triggered: always creates a brand-new Skillport account/session with the given password, regardless of any existing session's state.</summary>
    Task<SkillsoftProvisionResult> CreateNewSessionAsync(int userId, int companyId, string password, CancellationToken cancellationToken = default);
}
