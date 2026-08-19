using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Identity;

/// <summary>Maps to the existing "Companies" table. Narrow write path added for company
/// creation (see CreateCompanyCommandHandler), plus editing name/code and soft-delete
/// (IsActive) via UpdateDetails/Deactivate/Activate. A company also carries a Trial/License
/// coverage window (PlanType/PlanStartDate/PlanEndDate) - access is blocked once PlanEndDate
/// passes, independently of the manual IsActive toggle (see UserDirectory.QueryActiveCompanyRoles).</summary>
public class Company : IAggregateRoot
{
    public const string TrialPlan = "Trial";
    public const string LicensePlan = "License";

    /// <summary>Length of a Trial plan's coverage window, starting the day the company is created.</summary>
    public const int TrialDurationDays = 14;

    public int CompanyId { get; private set; }

    public string CompanyCode { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string? CompanyEmail { get; private set; }

    public string? CompanyPhone { get; private set; }

    public string? LogoUrl { get; private set; }

    public bool IsActive { get; private set; }

    public string PlanType { get; private set; } = TrialPlan;

    public DateOnly PlanStartDate { get; private set; }

    public DateOnly PlanEndDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>True once the current Trial/License window has passed - evaluated in-memory only
    /// (never inline this in an EF query; compare PlanEndDate directly there instead so it stays
    /// server-translatable).</summary>
    public bool IsExpired => PlanEndDate < DateOnly.FromDateTime(DateTime.UtcNow);

    private Company()
    {
    }

    /// <summary>planType must be Company.TrialPlan or Company.LicensePlan. For a Trial, the window is
    /// always "today" through +TrialDurationDays. For a License, licenseStartDate/licenseEndDate are
    /// required and used as-is (validated by CreateCompanyCommandValidator).</summary>
    public static Company Create(
        string companyCode,
        string companyName,
        string? companyEmail,
        string? companyPhone,
        string planType,
        DateOnly? licenseStartDate,
        DateOnly? licenseEndDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isTrial = planType != LicensePlan;

        return new Company
        {
            CompanyCode = companyCode,
            CompanyName = companyName,
            CompanyEmail = companyEmail,
            CompanyPhone = companyPhone,
            IsActive = true,
            PlanType = isTrial ? TrialPlan : LicensePlan,
            PlanStartDate = isTrial ? today : licenseStartDate!.Value,
            PlanEndDate = isTrial ? today.AddDays(TrialDurationDays) : licenseEndDate!.Value,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateDetails(string companyCode, string companyName)
    {
        CompanyCode = companyCode;
        CompanyName = companyName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLicense(DateOnly startDate, DateOnly endDate)
    {
        PlanType = LicensePlan;
        PlanStartDate = startDate;
        PlanEndDate = endDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateLogo(string? logoUrl)
    {
        LogoUrl = logoUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
