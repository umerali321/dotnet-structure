using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public class SkillsoftProvisioningService : ISkillsoftProvisioningService
{
    /// <summary>Skillport's own account API limit - unrelated to the table below.</summary>
    private const int LegacyColumnMaxLength = 50;

    // The REAL widths of dbo.ActiveLibraryCards, taken from the live schema rather than assumed.
    // They are not uniform, and treating them as a flat 50 is what rejected valid company names.
    private const int CompanyCodeMaxLength = 50;   // Company_Code
    private const int CompanyNameMaxLength = 100;  // Company_Name  <- not 50
    private const int ManagerIdMaxLength = 50;     // Manager_ID
    private const int NameMaxLength = 50;          // First_Name / Last_Name
    private const int EmailMaxLength = 50;         // Email
    private const int UserIdMaxLength = 50;        // User_ID
    private const int PasswordMaxLength = 50;      // Password
    private const int FdmNameMaxLength = 50;       // FDM_Name

    /// <summary>Trims a display-only value to what its column can hold. Only ever applied to fields
    /// nothing joins on - a matching key is rejected instead, never quietly shortened.</summary>
    private static string? Fit(string? value, int maxLength) =>
        value is not null && value.Length > maxLength ? value[..maxLength] : value;

    private readonly SkillsoftProvisioningClient _client;
    private readonly ApplicationDbContext _dbContext;

    public SkillsoftProvisioningService(SkillsoftProvisioningClient client, ApplicationDbContext dbContext)
    {
        _client = client;
        _dbContext = dbContext;
    }

    public async Task<SkillsoftProvisionResult> CreateAccountAsync(
        string username, string password, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var tooLong = new[] { username, password, firstName, lastName }.Any(v => v.Length > LegacyColumnMaxLength);
        if (tooLong)
        {
            return new SkillsoftProvisionResult(false, $"One of the account fields is too long for Skillport (max {LegacyColumnMaxLength} characters).");
        }

        var apiResult = await _client.CreateUserAsync(username, password, firstName, lastName, cancellationToken);
        return apiResult.Success
            ? new SkillsoftProvisionResult(true, null)
            : new SkillsoftProvisionResult(false, apiResult.ErrorMessage);
    }

    public async Task<SkillsoftProvisionResult> RecordEntitlementAsync(SkillsoftEntitlementRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies.AsNoTracking()
            .Where(c => c.CompanyId == request.CompanyId)
            .Select(c => new { c.CompanyCode, c.CompanyName })
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return new SkillsoftProvisionResult(false, "Company not found.");
        }

        // Two things were wrong here.
        //
        // FIRST: these columns are NOT all nvarchar(50). Company_Name is nvarchar(100) (verified
        // against the live schema), so a company with a 51-100 character name was refused a session
        // for a limit that does not exist - "CompanyName is too long for Skillport (max 50
        // characters)" on a name the column would have stored perfectly well.
        //
        // SECOND: refusing is only the right answer for the fields that are MATCHING KEYS. Company
        // Code, Email and Username are looked up by exact value (entitlement matching, and the
        // transcript import's ActiveLibraryCards path), so a truncated one silently fails to match
        // and must be rejected loudly instead. Company_Name, the names and the manager fields are
        // carried for display only - nothing joins on them - so truncating to fit is far better
        // than blocking a legitimate user from starting a course over a long company name.
        var oversizedKey = new[]
            {
                (nameof(company.CompanyCode), company.CompanyCode, CompanyCodeMaxLength),
                (nameof(request.Username), request.Username, UserIdMaxLength),
                (nameof(request.Password), request.Password, PasswordMaxLength),
                (nameof(request.Email), request.Email, EmailMaxLength),
            }
            .FirstOrDefault(field => (field.Item2?.Length ?? 0) > field.Item3);

        if (oversizedKey.Item1 is not null)
        {
            return new SkillsoftProvisionResult(false,
                $"{oversizedKey.Item1} is too long for Skillport (max {oversizedKey.Item3} characters). "
                + "This field is matched exactly, so it cannot be shortened automatically.");
        }

        var companyName = Fit(company.CompanyName, CompanyNameMaxLength);
        var firstName = Fit(request.FirstName, NameMaxLength);
        var lastName = Fit(request.LastName, NameMaxLength);
        var managerEmail = Fit(request.ManagerEmail, ManagerIdMaxLength);
        var managerName = Fit(request.ManagerName, FdmNameMaxLength);

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(30);

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO dbo.ActiveLibraryCards
                 (Company_Code, Company_Name, Manager_ID, First_Name, Last_Name, Email, User_ID, Password, Start_Date, End_Date, FDM_Name)
             VALUES
                 ({company.CompanyCode}, {companyName}, {managerEmail}, {firstName}, {lastName}, {request.Email}, {request.Username}, {request.Password}, {startDate}, {endDate}, {managerName})
             """,
            cancellationToken);

        return new SkillsoftProvisionResult(true, null);
    }

    public async Task<SkillsoftProvisionResult> ProvisionAsync(SkillsoftProvisionRequest request, CancellationToken cancellationToken = default)
    {
        var accountResult = await CreateAccountAsync(request.Username, request.Password, request.FirstName, request.LastName, cancellationToken);
        if (!accountResult.Success)
        {
            return accountResult;
        }

        return await RecordEntitlementAsync(
            new SkillsoftEntitlementRequest(
                request.CompanyId, request.Username, request.Password, request.FirstName, request.LastName,
                request.Email, request.ManagerEmail, request.ManagerName),
            cancellationToken);
    }
}
