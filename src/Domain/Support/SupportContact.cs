using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Support;

/// <summary>
/// A brand-new table (not a legacy one) backing the "Customer Service" panel on the FAQ / Support
/// page. CompanyId is nullable to support a platform-wide/global contact visible to every company,
/// alongside company-scoped ones - same pattern as <see cref="Faq"/>.
/// </summary>
public class SupportContact : IAggregateRoot
{
    public int SupportContactId { get; private set; }

    public int? CompanyId { get; private set; }

    public string ContactType { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? UpdatedBy { get; private set; }

    private SupportContact()
    {
    }

    public static SupportContact Create(int? companyId, string contactType, string value, int sortOrder, string createdBy)
    {
        return new SupportContact
        {
            CompanyId = companyId,
            ContactType = contactType,
            Value = value,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }

    public void Update(string contactType, string value, int sortOrder, string updatedBy)
    {
        ContactType = contactType;
        Value = value;
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
