using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Identity;

/// <summary>Maps to the existing "Companies" table. Narrow write path added for company
/// creation (see CreateCompanyCommandHandler) - still no update/delete beyond that.</summary>
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

    public static Company Create(string companyCode, string companyName, string? companyEmail, string? companyPhone)
    {
        return new Company
        {
            CompanyCode = companyCode,
            CompanyName = companyName,
            CompanyEmail = companyEmail,
            CompanyPhone = companyPhone,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
