using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Skillsoft;

/// <summary>
/// Our own normalized record of a user's Skillport sessions - one row per session. Unlike the legacy
/// ActiveLibraryCards table (no real key, matched by email/company code, Start_Date/End_Date are
/// NOT NULL so it can't represent "account exists but session not started yet"), this table has a
/// real UserId FK and allows null dates for that dormant state. It is also the reliable source for
/// "how many times has this user had a session" (client usage), since ActiveLibraryCards data can
/// contain duplicates/anomalies that make it unsafe to count directly.
/// </summary>
public class SkillportSession : IAggregateRoot
{
    public int SkillportSessionId { get; private set; }

    public int UserId { get; private set; }

    public int CompanyId { get; private set; }

    public string SkillportUsername { get; private set; } = string.Empty;

    public string SkillportPassword { get; private set; } = string.Empty;

    /// <summary>Null until the session is actually activated (first "Access the course library" click).</summary>
    public DateTime? StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ActivatedAt { get; private set; }

    private SkillportSession()
    {
    }

    /// <summary>A Skillport account has been created but the 30-day clock hasn't started yet.</summary>
    public static SkillportSession CreateDormant(int userId, int companyId, string skillportUsername, string skillportPassword)
    {
        return new SkillportSession
        {
            UserId = userId,
            CompanyId = companyId,
            SkillportUsername = skillportUsername,
            SkillportPassword = skillportPassword,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>A brand-new session that starts already-active (used when there was no dormant account to activate - e.g. a first-ever self-service click, or a restart after expiry).</summary>
    public static SkillportSession CreateActive(int userId, int companyId, string skillportUsername, string skillportPassword, DateTime startDate, DateTime endDate)
    {
        var session = CreateDormant(userId, companyId, skillportUsername, skillportPassword);
        session.Activate(startDate, endDate);
        return session;
    }

    public bool IsDormant => StartDate is null || EndDate is null;

    public bool IsExpired(DateTime today) => !IsDormant && EndDate < today;

    public bool IsActive(DateTime today) => !IsDormant && StartDate <= today && EndDate >= today;

    public void Activate(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
        ActivatedAt = DateTimeOffset.UtcNow;
    }
}
