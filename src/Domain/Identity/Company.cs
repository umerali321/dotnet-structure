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

    public int CompanyId { get; private set; }

    public string CompanyCode { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string? CompanyEmail { get; private set; }

    public string? CompanyPhone { get; private set; }

    public string? Street1 { get; private set; }

    public string? Street2 { get; private set; }

    public string? City { get; private set; }

    public string? State { get; private set; }

    public string? Zip { get; private set; }

    public string? PaymentForm { get; private set; }

    public decimal? TotalPayment { get; private set; }

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

    /// <summary>planType must be Company.TrialPlan or Company.LicensePlan - licenseStartDate/
    /// licenseEndDate are required and used as-is for either (validated by
    /// CreateCompanyCommandValidator). A Trial's window is caller-chosen, not a fixed 14 days -
    /// once it passes, this company can never receive another Trial: the only way to extend
    /// coverage after that is SetLicense, which always converts to License, never back to Trial.</summary>
    public static Company Create(
        string companyCode,
        string companyName,
        string? companyEmail,
        string? companyPhone,
        string planType,
        DateOnly? licenseStartDate,
        DateOnly? licenseEndDate,
        string? street1 = null,
        string? street2 = null,
        string? city = null,
        string? state = null,
        string? zip = null,
        string? paymentForm = null,
        decimal? totalPayment = null)
    {
        var isTrial = planType != LicensePlan;

        return new Company
        {
            CompanyCode = companyCode,
            CompanyName = companyName,
            CompanyEmail = companyEmail,
            CompanyPhone = companyPhone,
            Street1 = street1,
            Street2 = street2,
            City = city,
            State = state,
            Zip = zip,
            PaymentForm = paymentForm,
            TotalPayment = totalPayment,
            IsActive = true,
            PlanType = isTrial ? TrialPlan : LicensePlan,
            PlanStartDate = licenseStartDate!.Value,
            PlanEndDate = licenseEndDate!.Value,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateDetails(
        string companyCode,
        string companyName,
        string? companyEmail = null,
        string? companyPhone = null,
        string? street1 = null,
        string? street2 = null,
        string? city = null,
        string? state = null,
        string? zip = null,
        string? paymentForm = null,
        decimal? totalPayment = null)
    {
        CompanyCode = companyCode;
        CompanyName = companyName;
        CompanyEmail = companyEmail;
        CompanyPhone = companyPhone;
        Street1 = street1;
        Street2 = street2;
        City = city;
        State = state;
        Zip = zip;
        PaymentForm = paymentForm;
        TotalPayment = totalPayment;
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
