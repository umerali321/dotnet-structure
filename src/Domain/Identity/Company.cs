using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Identity;

/// <summary>Maps to the existing "Companies" table. Read-only for now.</summary>
public class Company : IAggregateRoot
{
    public int CompanyId { get; private set; }

    public string CompanyCode { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string? CompanyEmail { get; private set; }

    public string? CompanyPhone { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private Company()
    {
    }
}
