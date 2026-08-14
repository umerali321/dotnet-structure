using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public class SkillsoftProvisioningService : ISkillsoftProvisioningService
{
    private const int LegacyColumnMaxLength = 50;

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

        // dbo.ActiveLibraryCards columns are all nvarchar(50) - reject up front rather than let SQL
        // truncate silently, which would break entitlement matching (exact CompanyCode/Email lookup).
        var tooLong = new[]
            {
                (nameof(company.CompanyCode), company.CompanyCode),
                (nameof(company.CompanyName), company.CompanyName),
                (nameof(request.Username), request.Username),
                (nameof(request.Password), request.Password),
                (nameof(request.FirstName), request.FirstName),
                (nameof(request.LastName), request.LastName),
                (nameof(request.Email), request.Email),
                (nameof(request.ManagerEmail), request.ManagerEmail),
                (nameof(request.ManagerName), request.ManagerName),
            }
            .FirstOrDefault(field => field.Item2.Length > LegacyColumnMaxLength);

        if (tooLong.Item1 is not null)
        {
            return new SkillsoftProvisionResult(false, $"{tooLong.Item1} is too long for Skillport (max {LegacyColumnMaxLength} characters).");
        }

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(30);

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO dbo.ActiveLibraryCards
                 (Company_Code, Company_Name, Manager_ID, First_Name, Last_Name, Email, User_ID, Password, Start_Date, End_Date, FDM_Name)
             VALUES
                 ({company.CompanyCode}, {company.CompanyName}, {request.ManagerEmail}, {request.FirstName}, {request.LastName}, {request.Email}, {request.Username}, {request.Password}, {startDate}, {endDate}, {request.ManagerName})
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
