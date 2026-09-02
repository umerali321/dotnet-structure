using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Domain.RosterImport;

namespace SkillsetsBackend.Application.RosterImport.Interfaces;

/// <summary>An account this roster is about to touch, as it already exists in the database.</summary>
/// <param name="HasStudentRoleAtCompany">Whether they are already an Employee at the target company.</param>
/// <param name="HasManagerRoleAtCompany">Whether they already have Manager access there - the
/// difference between "nothing to do" and "grant the manager dashboard they asked for".</param>
public record ExistingUserLookup(
    int UserId,
    string Email,
    bool HasStudentRoleAtCompany,
    bool HasManagerRoleAtCompany);

/// <summary>A user created by a batch, with the credentials the welcome email needs.</summary>
public record RosterCreatedUser(int UserId, string Email, string? FirstName, string Password);

public interface IRosterImportRepository
{
    /// <summary>
    /// Looks up every email in the file in ONE round trip (chunked internally) rather than a query
    /// per row - a 20,000-row roster would otherwise be 20,000 queries. Only the columns actually
    /// needed are projected; never the whole entity.
    /// </summary>
    Task<IReadOnlyDictionary<string, ExistingUserLookup>> FindExistingUsersByEmailAsync(
        IReadOnlyCollection<string> emails,
        int companyId,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves a company by name (exact, then case-insensitive trim match). Null when the
    /// name matches nothing - the import then refuses rather than inventing a company.</summary>
    Task<(int CompanyId, string CompanyName)?> FindCompanyByNameAsync(string companyName, CancellationToken cancellationToken = default);

    Task<(int CompanyId, string CompanyName)?> FindCompanyByIdAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates one person's account and role rows inside a single transaction, so a row that fails
    /// half-way leaves nothing behind. Returns the new UserId.
    /// </summary>
    /// <param name="alsoManager">Grant Manager access at the same company as well as Employee -
    /// the "Give Mgr Dashboard = Yes" rule.</param>
    Task<int> CreateRosterUserAsync(
        string email,
        string? phone,
        string firstName,
        string lastName,
        string password,
        string employeeType,
        int companyId,
        bool alsoManager,
        string createdByEmail,
        CancellationToken cancellationToken = default);

    /// <summary>Grants Manager access to somebody who already exists - used when a roster row asks
    /// for the manager dashboard for a person already on the books. Never creates a second Users
    /// row.</summary>
    Task GrantManagerRoleAsync(int userId, int companyId, CancellationToken cancellationToken = default);

    Task<int> SaveBatchAsync(RosterImportBatch batch, CancellationToken cancellationToken = default);

    Task<RosterImportBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default);

    /// <summary>The accounts a batch created, with the password actually stored for each - read back
    /// from Users rather than kept in a second place. Drives the deferred welcome emails.</summary>
    Task<IReadOnlyList<RosterCreatedUser>> GetCreatedUsersForBatchAsync(int batchId, CancellationToken cancellationToken = default);

    Task MarkWelcomeEmailsSentAsync(int batchId, int sentCount, CancellationToken cancellationToken = default);

    /// <summary>Manual vs roster-import counts for Employees and Managers, straight off the stored
    /// CreationSource columns.</summary>
    Task<CreationSourceStatsDto> GetCreationSourceStatsAsync(
        IReadOnlyCollection<int>? restrictToCompanyIds,
        CancellationToken cancellationToken = default);
}
