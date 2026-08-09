namespace SkillsetsBackend.Domain.Support;

public static class SupportRequestStatus
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";

    public static readonly IReadOnlyCollection<string> All = [Open, InProgress, Resolved];

    public static bool IsValid(string status) => All.Contains(status);
}
