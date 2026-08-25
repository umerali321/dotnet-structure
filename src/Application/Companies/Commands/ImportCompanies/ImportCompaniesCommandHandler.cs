using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.DTOs;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Companies.Commands.ImportCompanies;

/// <summary>SuperAdmin only. For every row: find the company by Company Code (then, failing that,
/// Company Name); if it doesn't exist, create it + its Company Admin exactly like the single-company
/// "Add Company" flow (CreateCompanyCommandHandler) does; if it already exists, only fill in
/// currently-missing fields from the row and never overwrite anything already populated, and only
/// create/complete a Company Admin if the company doesn't already have one. Never invents license/
/// date values - only ever uses what the file actually contains. One row's failure never aborts the
/// rest of the file (each row is processed and reported independently).</summary>
public class ImportCompaniesCommandHandler
{
    private const int MaxRows = 5000;

    // Companies.PlanStartDate/PlanEndDate are non-nullable DateOnly columns, backfilled with these
    // exact values by the migration that added them (see CompanyConfiguration's HasDefaultValue
    // comments) - a company still sitting on both of these means its license window was never
    // actually set, so it's safe to treat as "missing" and fill in from the import file.
    private static readonly DateOnly UnsetPlanStartDate = new(2020, 1, 1);
    private static readonly DateOnly UnsetPlanEndDate = new(2099, 12, 31);

    // Excel dates in this file are DD/MM/YYYY (per the spec's own example: "25/11/2023"/"24/11/2026" -
    // day > 12 confirms day-first order) - tried first so an ambiguous date like "03/04/2023" resolves
    // the same way the source file intends, falling back to US month-first and ISO only if day-first
    // parsing rejects the value outright (e.g. day > 31).
    private static readonly string[] DateFormats =
    [
        "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "M/d/yyyy", "MM/dd/yyyy",
    ];

    private readonly ICompanyRepository _companyRepository;
    private readonly IManagerRepository _managerRepository;

    public ImportCompaniesCommandHandler(ICompanyRepository companyRepository, IManagerRepository managerRepository)
    {
        _companyRepository = companyRepository;
        _managerRepository = managerRepository;
    }

    public async Task<ImportResultDto> Handle(ImportCompaniesCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can import companies.");
        }

        if (command.Rows.Count > MaxRows)
        {
            throw new AppValidationException([new ValidationFailure("File", $"This file has {command.Rows.Count} rows - the import tool supports at most {MaxRows} rows per file.")]);
        }

        var results = new List<ImportRowResultDto>();
        var codeFirstSeenAtRow = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var nameFirstSeenAtRow = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int created = 0, updated = 0, adminsCreated = 0, adminsUpdated = 0, noChanges = 0, validationErrors = 0, failed = 0;

        foreach (var row in command.Rows)
        {
            try
            {
                var parsed = ParseRow(row);

                if (parsed.CompanyCode is not null && codeFirstSeenAtRow.TryGetValue(parsed.CompanyCode, out var firstCodeRow))
                {
                    validationErrors++;
                    results.Add(new ImportRowResultDto(row.RowNumber, parsed.CompanyCode, parsed.CompanyName, "ValidationError",
                        $"Duplicate Company Code - already processed at row {firstCodeRow} in this file.", parsed.Warnings));
                    continue;
                }

                if (parsed.CompanyName is not null && nameFirstSeenAtRow.TryGetValue(parsed.CompanyName, out var firstNameRow))
                {
                    validationErrors++;
                    results.Add(new ImportRowResultDto(row.RowNumber, parsed.CompanyCode, parsed.CompanyName, "ValidationError",
                        $"Duplicate Company Name - already processed at row {firstNameRow} in this file.", parsed.Warnings));
                    continue;
                }

                if (parsed.CompanyCode is not null) codeFirstSeenAtRow[parsed.CompanyCode] = row.RowNumber;
                if (parsed.CompanyName is not null) nameFirstSeenAtRow[parsed.CompanyName] = row.RowNumber;

                var existing = (parsed.CompanyCode is not null || parsed.CompanyName is not null)
                    ? await _companyRepository.FindExistingAsync(parsed.CompanyCode ?? string.Empty, parsed.CompanyName ?? string.Empty, cancellationToken)
                    : null;

                if (existing is null)
                {
                    var missing = GetMissingRequiredFieldsForNewCompany(parsed);
                    if (missing.Count > 0)
                    {
                        validationErrors++;
                        results.Add(new ImportRowResultDto(row.RowNumber, parsed.CompanyCode, parsed.CompanyName, "ValidationError",
                            "Missing/invalid required field(s): " + string.Join(", ", missing), parsed.Warnings));
                        continue;
                    }

                    var company = Company.Create(
                        parsed.CompanyCode!, parsed.CompanyName!, parsed.Email, parsed.Phone,
                        Company.LicensePlan, parsed.StartDate, parsed.EndDate,
                        parsed.Street1, parsed.Street2, parsed.City, parsed.State, parsed.Zip,
                        parsed.PaymentForm, parsed.TotalPayment, parsed.PurchaseDate);
                    var admin = AppUser.CreateStudent(
                        parsed.Email!, phone: null, parsed.AdminFirstName!, parsed.AdminLastName!, parsed.Email!, GenerateRandomPassword());

                    await _companyRepository.CreateCompanyWithAdminAsync(company, admin, cancellationToken);

                    created++;
                    adminsCreated++;
                    results.Add(new ImportRowResultDto(row.RowNumber, parsed.CompanyCode, parsed.CompanyName, "Created",
                        "New company and Company Admin created.", parsed.Warnings));
                    continue;
                }

                var companyChanged = false;

                var companyEmail = existing.CompanyEmail;
                var companyPhone = existing.CompanyPhone;
                var street1 = existing.Street1;
                var street2 = existing.Street2;
                var city = existing.City;
                var state = existing.State;
                var zip = existing.Zip;
                var paymentForm = existing.PaymentForm;
                var totalPayment = existing.TotalPayment;

                if (string.IsNullOrWhiteSpace(companyEmail) && parsed.Email is not null) { companyEmail = parsed.Email; companyChanged = true; }
                if (string.IsNullOrWhiteSpace(companyPhone) && parsed.Phone is not null) { companyPhone = parsed.Phone; companyChanged = true; }
                if (string.IsNullOrWhiteSpace(street1) && parsed.Street1 is not null) { street1 = parsed.Street1; companyChanged = true; }
                if (string.IsNullOrWhiteSpace(street2) && parsed.Street2 is not null) { street2 = parsed.Street2; companyChanged = true; }
                if (string.IsNullOrWhiteSpace(city) && parsed.City is not null) { city = parsed.City; companyChanged = true; }
                if (string.IsNullOrWhiteSpace(state) && parsed.State is not null) { state = parsed.State; companyChanged = true; }
                if (string.IsNullOrWhiteSpace(zip) && parsed.Zip is not null) { zip = parsed.Zip; companyChanged = true; }
                if (string.IsNullOrWhiteSpace(paymentForm) && parsed.PaymentForm is not null) { paymentForm = parsed.PaymentForm; companyChanged = true; }
                if (totalPayment is null && parsed.TotalPayment is not null) { totalPayment = parsed.TotalPayment; companyChanged = true; }

                if (companyChanged)
                {
                    existing.UpdateDetails(existing.CompanyCode, existing.CompanyName, companyEmail, companyPhone, street1, street2, city, state, zip, paymentForm, totalPayment);
                }

                var datesNeverSet = existing.PlanStartDate == UnsetPlanStartDate && existing.PlanEndDate == UnsetPlanEndDate;
                if (datesNeverSet && parsed.StartDate is not null && parsed.EndDate is not null && parsed.EndDate > parsed.StartDate)
                {
                    // SetLicense is the only domain method that can set these dates, and it also
                    // stamps PlanType = License - acceptable here since a company still sitting on
                    // the unset sentinel dates was never meaningfully classified either way, and
                    // License is this tool's documented default for imported companies.
                    existing.SetLicense(parsed.StartDate.Value, parsed.EndDate.Value);
                    companyChanged = true;
                }

                if (existing.PurchaseDate is null && parsed.PurchaseDate is not null)
                {
                    existing.SetPurchaseDate(parsed.PurchaseDate.Value);
                    companyChanged = true;
                }

                var adminAction = "None";

                if (await _companyRepository.HasActiveCompanyAdminAsync(existing.CompanyId, cancellationToken))
                {
                    var currentAdmin = await _companyRepository.GetCompanyAdminAsync(existing.CompanyId, cancellationToken);
                    if (currentAdmin is not null
                        && (string.IsNullOrWhiteSpace(currentAdmin.FirstName) || string.IsNullOrWhiteSpace(currentAdmin.LastName))
                        && parsed.AdminFirstName is not null && parsed.AdminLastName is not null)
                    {
                        currentAdmin.UpdatePersonalInfo(
                            string.IsNullOrWhiteSpace(currentAdmin.FirstName) ? parsed.AdminFirstName : currentAdmin.FirstName!,
                            string.IsNullOrWhiteSpace(currentAdmin.LastName) ? parsed.AdminLastName : currentAdmin.LastName!,
                            currentAdmin.Phone);
                        adminAction = "Updated";
                    }
                }
                else if (parsed.Email is not null && parsed.AdminFirstName is not null && parsed.AdminLastName is not null)
                {
                    var existingUser = await _companyRepository.FindUserByEmailAsync(parsed.Email, cancellationToken);
                    if (existingUser is not null)
                    {
                        await _managerRepository.AddCompanyAdminRoleAsync(existingUser.UserId, existing.CompanyId, startDate: null, cancellationToken);
                    }
                    else
                    {
                        var newAdmin = AppUser.CreateStudent(parsed.Email, phone: null, parsed.AdminFirstName, parsed.AdminLastName, parsed.Email, GenerateRandomPassword());
                        await _managerRepository.CreateManagerAsync(newAdmin, existing.CompanyId, startDate: null, Roles.CompanyAdmin, cancellationToken);
                    }

                    adminAction = "Created";
                }
                else
                {
                    parsed.Warnings.Add("Could not create a Company Admin - Point of Contact name and/or email is missing.");
                }

                if (companyChanged || adminAction == "Updated")
                {
                    // Flushes both the company field-fill (if any) and currentAdmin.UpdatePersonalInfo
                    // (if any) in one round trip - they're tracked by the same DbContext instance. The
                    // "Created" admin path already persisted itself via its own repository call, so it
                    // doesn't need to be included in this condition.
                    await _companyRepository.SaveChangesAsync(cancellationToken);
                }

                if (adminAction == "Created") adminsCreated++;
                if (adminAction == "Updated") adminsUpdated++;

                if (companyChanged || adminAction != "None")
                {
                    updated++;
                    results.Add(new ImportRowResultDto(row.RowNumber, parsed.CompanyCode, parsed.CompanyName, "Updated",
                        BuildUpdateMessage(companyChanged, adminAction), parsed.Warnings));
                }
                else
                {
                    noChanges++;
                    results.Add(new ImportRowResultDto(row.RowNumber, parsed.CompanyCode, parsed.CompanyName, "NoChangesRequired",
                        "Company already exists with no missing information to fill in.", parsed.Warnings));
                }
            }
            catch (Exception ex)
            {
                failed++;
                results.Add(new ImportRowResultDto(row.RowNumber, row.CoCode, row.CompanyName, "ImportFailed", ex.Message, []));
            }
        }

        var summary = new ImportSummaryDto(command.Rows.Count, created, updated, adminsCreated, adminsUpdated, noChanges, validationErrors, failed);
        return new ImportResultDto(summary, results);
    }

    private static string BuildUpdateMessage(bool companyChanged, string adminAction) => (companyChanged, adminAction) switch
    {
        (true, "Created") => "Filled in missing company information; Company Admin created.",
        (true, "Updated") => "Filled in missing company information; Company Admin info completed.",
        (true, "None") => "Filled in missing company information.",
        (false, "Created") => "Company Admin created.",
        (false, "Updated") => "Company Admin info completed.",
        _ => "Updated.",
    };

    private static List<string> GetMissingRequiredFieldsForNewCompany(ParsedRow parsed)
    {
        var missing = new List<string>();
        if (parsed.CompanyCode is null) missing.Add("Co Code");
        if (parsed.CompanyName is null) missing.Add("Company Name");
        if (parsed.Email is null) missing.Add("Email");
        if (parsed.Phone is null) missing.Add("Phone");
        if (parsed.Street1 is null) missing.Add("Street 1");
        if (parsed.State is null) missing.Add("State");
        if (parsed.Zip is null) missing.Add("Zip");
        if (parsed.StartDate is null) missing.Add("Start Date");
        if (parsed.EndDate is null) missing.Add("Expiration Date");
        if (parsed.StartDate is not null && parsed.EndDate is not null && parsed.EndDate <= parsed.StartDate)
        {
            missing.Add("Expiration Date (must be after Start Date)");
        }
        if (parsed.AdminFirstName is null || parsed.AdminLastName is null) missing.Add("Point of Contact (first and last name)");
        return missing;
    }

    private static ParsedRow ParseRow(ImportRawRow row)
    {
        var parsed = new ParsedRow
        {
            CompanyCode = Trim(row.CoCode),
            CompanyName = Trim(row.CompanyName),
            Phone = Trim(row.Phone),
            Street1 = Trim(row.Street1),
            Street2 = Trim(row.Street2),
            City = Trim(row.City),
            State = Trim(row.State),
            Zip = Trim(row.Zip),
            PaymentForm = Trim(row.PaymentForm),
        };

        var email = Trim(row.Email);
        if (email is not null && !MailAddress.TryCreate(email, out _))
        {
            parsed.Warnings.Add($"Email '{email}' is not a valid email address and was ignored.");
            email = null;
        }

        parsed.Email = email;

        var totalPaymentRaw = Trim(row.TotalPayment);
        if (totalPaymentRaw is not null)
        {
            if (decimal.TryParse(totalPaymentRaw, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out var totalPayment))
            {
                parsed.TotalPayment = totalPayment;
            }
            else
            {
                parsed.Warnings.Add($"Total Payment '{totalPaymentRaw}' could not be read as a number and was left blank.");
            }
        }

        parsed.StartDate = ParseDate(row.StartDate, "Start Date", parsed.Warnings);
        parsed.EndDate = ParseDate(row.ExpirationDate, "Expiration Date", parsed.Warnings);
        parsed.PurchaseDate = ParseDate(row.PurchaseDate, "Purchase Date", parsed.Warnings);

        var contact = Trim(row.PointOfContact);
        if (contact is not null)
        {
            var tokens = contact.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length >= 2)
            {
                parsed.AdminFirstName = tokens[0];
                parsed.AdminLastName = string.Join(' ', tokens[1..]);
            }
            else
            {
                parsed.Warnings.Add($"Point of Contact '{contact}' could not be split into a first and last name.");
            }
        }

        return parsed;
    }

    private static DateOnly? ParseDate(string? raw, string fieldLabel, List<string> warnings)
    {
        var value = Trim(raw);
        if (value is null)
        {
            return null;
        }

        if (DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        warnings.Add($"{fieldLabel} '{value}' could not be read as a date and was left blank.");
        return null;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Same 8-digit numeric temp-password convention already used by
    /// ResetPasswordCommandHandler/SkillportSessionManager.</summary>
    private static string GenerateRandomPassword()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 100_000_000;
        return value.ToString("D8");
    }

    private sealed class ParsedRow
    {
        public string? CompanyCode { get; set; }

        public string? CompanyName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Street1 { get; set; }

        public string? Street2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Zip { get; set; }

        public string? PaymentForm { get; set; }

        public decimal? TotalPayment { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public DateOnly? PurchaseDate { get; set; }

        public string? AdminFirstName { get; set; }

        public string? AdminLastName { get; set; }

        public List<string> Warnings { get; } = [];
    }
}
