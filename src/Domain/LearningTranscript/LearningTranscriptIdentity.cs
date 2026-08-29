using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.LearningTranscript;

/// <summary>One row per distinct Skillport account seen across every imported report - the
/// resolution of "which of our own Users.UserId is this Skillport username" happens once here and
/// is reused by every later import, instead of re-matching on every batch. UserId is null until a
/// confident match is found (via SkillportSessions, then ActiveLibraryCards - see
/// sp_ImportLearningTranscriptBatch); a null UserId means this identity is surfaced in the
/// "Unmatched" reconciliation view instead of the main scoped report, since we can't safely
/// determine which company/manager it belongs to without a resolved user. The raw
/// GroupName/GroupOrgCode/ApprovalManager* fields are exactly what Skillport itself reported and
/// are display/audit-only - real access scoping always goes through our own UserCompanyRoles /
/// StudentProfiles.ManagerId once UserId is resolved, never through these text fields.</summary>
public class LearningTranscriptIdentity : IAggregateRoot
{
    public int LearningTranscriptIdentityId { get; private set; }

    public string SkillportUsername { get; private set; } = string.Empty;

    public int? UserId { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? DisplayFirstName { get; private set; }

    public string? DisplayLastName { get; private set; }

    public string? Location { get; private set; }

    public string? UserStatus { get; private set; }

    public string? GroupName { get; private set; }

    public string? GroupOrgCode { get; private set; }

    /// <summary>The report's hierarchical group location (e.g.
    /// "/Skillsets Online of Silicon Valley/Library Card") - display/audit-only, like GroupName/
    /// GroupOrgCode; not every report includes this column.</summary>
    public string? GroupPath { get; private set; }

    public string? ApprovalManagerId { get; private set; }

    public string? ApprovalManagerFirstName { get; private set; }

    public string? ApprovalManagerLastName { get; private set; }

    /// <summary>"SkillportSession", "ActiveLibraryCard", or "Manual" - which lookup actually
    /// resolved UserId, kept for audit/debugging when a match looks wrong.</summary>
    public string? ResolutionMethod { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private LearningTranscriptIdentity()
    {
    }
}
