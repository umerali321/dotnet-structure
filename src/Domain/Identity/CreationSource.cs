namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// How an account or a role grant came into existence. Stored explicitly rather than inferred later
/// from timestamps or batch sizes, so "how many employees were created by roster import vs by hand"
/// is a straight GROUP BY instead of a guess.
///
/// Recorded in two places, because they answer different questions:
///   AppUser.CreationSource          - how the PERSON's account was first created.
///   UserCompanyRole.CreationSource  - how THAT role grant was made. A person can be created
///                                     manually as an Employee and later granted Manager by a
///                                     roster import; only the per-role value can tell you that.
/// </summary>
public static class CreationSource
{
    /// <summary>Created one at a time through the admin UI or API.</summary>
    public const string Manual = "Manual";

    /// <summary>Created by the bulk Employee Roster Import.</summary>
    public const string RosterImport = "RosterImport";

    /// <summary>Created by the Company Import tool (company + its Company Admin).</summary>
    public const string CompanyImport = "CompanyImport";

    /// <summary>Pre-existing rows from before source tracking was added. Distinct from Manual so a
    /// backfilled row is never miscounted as something we actually observed.</summary>
    public const string Legacy = "Legacy";

    public const int MaxLength = 20;

    public static bool IsKnown(string? value) =>
        value is Manual or RosterImport or CompanyImport or Legacy;
}
