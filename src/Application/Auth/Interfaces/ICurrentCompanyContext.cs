namespace SkillsetsBackend.Application.Auth.Interfaces;

/// <summary>The active company context (if any) for the current authenticated request.</summary>
public interface ICurrentCompanyContext
{
    int? CompanyId { get; }

    string? CompanyName { get; }

    /// <summary>The user's role for CompanyId (already normalized - Admin/Manager collapse to Manager).</summary>
    string? Role { get; }

    bool IsSuperAdmin { get; }
}
