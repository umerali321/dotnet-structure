namespace SkillsetsBackend.Application.Skillsoft.Interfaces;

/// <summary>
/// Application-layer entry point for the Skillport 30-day session lifecycle: admin-triggered
/// "create/retry now" with an explicit password. The self-service activate/expire/restart flow
/// (using the person's real app password and a random Skillport username) lives behind
/// ISkillsoftSsoService.StartSessionAsync instead, since it's tied to the launch-ticket flow.
/// </summary>
public interface ISkillportSessionService
{
    /// <summary>Admin-triggered: always creates a brand-new Skillport account/session with the given password, regardless of any existing session's state.</summary>
    Task<SkillsoftProvisionResult> CreateNewSessionAsync(int userId, int companyId, string password, CancellationToken cancellationToken = default);
}
