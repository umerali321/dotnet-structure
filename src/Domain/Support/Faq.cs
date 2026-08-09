using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Support;

/// <summary>
/// A brand-new table (not a legacy one) backing Settings -> FAQ Management. CompanyId is nullable
/// to support a platform-wide/global FAQ visible to every company, alongside company-scoped ones.
/// </summary>
public class Faq : IAggregateRoot
{
    public int FaqId { get; private set; }

    public int? CompanyId { get; private set; }

    public string Question { get; private set; } = string.Empty;

    public string Answer { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? UpdatedBy { get; private set; }

    private Faq()
    {
    }

    public static Faq Create(int? companyId, string question, string answer, string createdBy)
    {
        return new Faq
        {
            CompanyId = companyId,
            Question = question,
            Answer = answer,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }

    public void Update(string question, string answer, string updatedBy)
    {
        Question = question;
        Answer = answer;
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
