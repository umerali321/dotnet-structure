namespace SkillsetsBackend.Domain.Communications;

/// <summary>Stored as a plain string column (not a DB enum) so a future provider can be added
/// without a migration - same convention as Company.PlanType.</summary>
public static class SmtpProviderType
{
    public const string Gmail = "Gmail";
    public const string Microsoft365 = "Microsoft365";
    public const string Custom = "Custom";

    public static readonly IReadOnlyList<string> All = [Gmail, Microsoft365, Custom];

    public static bool IsKnown(string provider) => All.Contains(provider);
}
