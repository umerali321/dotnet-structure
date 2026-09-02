using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Application.RosterImport.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Domain.RosterImport;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.RosterImport;

public class RosterImportRepository : IRosterImportRepository
{
    /// <summary>SQL Server tops out around 2,100 parameters per statement, so a 20,000-email file
    /// has to go in slices rather than one enormous IN list.</summary>
    private const int EmailLookupChunkSize = 1000;

    private readonly ApplicationDbContext _dbContext;

    public RosterImportRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<string, ExistingUserLookup>> FindExistingUsersByEmailAsync(
        IReadOnlyCollection<string> emails,
        int companyId,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ExistingUserLookup>(StringComparer.OrdinalIgnoreCase);
        if (emails.Count == 0)
        {
            return result;
        }

        var studentRoleId = await GetRoleIdAsync(Roles.Student, cancellationToken);
        var managerRoleId = await GetRoleIdAsync(Roles.Manager, cancellationToken);

        foreach (var chunk in emails.Chunk(EmailLookupChunkSize))
        {
            // Projected to exactly the four values needed - never the whole entity, and never
            // SELECT *. The two role flags are computed in SQL so this stays one round trip per
            // chunk instead of one per user.
            var found = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Email != null && chunk.Contains(u.Email))
                .Select(u => new
                {
                    u.UserId,
                    Email = u.Email!,
                    HasStudent = _dbContext.UserCompanyRoles.Any(r =>
                        r.UserId == u.UserId && r.CompanyId == companyId && r.RoleId == studentRoleId && r.IsActive),
                    HasManager = _dbContext.UserCompanyRoles.Any(r =>
                        r.UserId == u.UserId && r.CompanyId == companyId && r.RoleId == managerRoleId && r.IsActive),
                })
                .ToListAsync(cancellationToken);

            foreach (var user in found)
            {
                result[user.Email] = new ExistingUserLookup(user.UserId, user.Email, user.HasStudent, user.HasManager);
            }
        }

        return result;
    }

    public async Task<(int CompanyId, string CompanyName)?> FindCompanyByNameAsync(
        string companyName, CancellationToken cancellationToken = default)
    {
        var trimmed = companyName.Trim();

        var match = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.CompanyName == trimmed)
            .Select(c => new { c.CompanyId, c.CompanyName })
            .FirstOrDefaultAsync(cancellationToken);

        // Falls back to a case/whitespace-insensitive comparison, because the organization name is
        // typed by hand into the template and rarely matches the stored name exactly.
        match ??= await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.CompanyName != null && c.CompanyName.Trim().ToLower() == trimmed.ToLower())
            .Select(c => new { c.CompanyId, c.CompanyName })
            .FirstOrDefaultAsync(cancellationToken);

        return match is null ? null : (match.CompanyId, match.CompanyName ?? trimmed);
    }

    public async Task<(int CompanyId, string CompanyName)?> FindCompanyByIdAsync(
        int companyId, CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .Select(c => new { c.CompanyId, c.CompanyName })
            .FirstOrDefaultAsync(cancellationToken);

        return match is null ? null : (match.CompanyId, match.CompanyName ?? string.Empty);
    }

    /// <summary>
    /// One person, one transaction. A row that fails part-way (say the Manager role insert violates
    /// a unique index) rolls back its own Users row too, so the import never leaves an account
    /// behind that the results table claims was not created.
    /// </summary>
    public async Task<int> CreateRosterUserAsync(
        string email,
        string? phone,
        string firstName,
        string lastName,
        string password,
        string employeeType,
        int companyId,
        bool alsoManager,
        string createdByEmail,
        CancellationToken cancellationToken = default)
    {
        var studentRoleId = await GetRoleIdAsync(Roles.Student, cancellationToken);
        var managerRoleId = alsoManager ? await GetRoleIdAsync(Roles.Manager, cancellationToken) : (byte)0;

        // EnableRetryOnFailure forbids a bare BeginTransactionAsync - the execution strategy has to
        // own the whole unit so it can retry it. Same pattern as StudentRepository.CreateStudentAsync.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Username mirrors the email, exactly as the single-employee create flow does.
            var user = AppUser.CreateStudent(email, phone, firstName, lastName, email, password,
                CreationSource.RosterImport);

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.StudentProfiles.Add(new StudentProfile(user.UserId, employeeType, createdByEmail));
            _dbContext.UserCompanyRoles.Add(
                new UserCompanyRole(user.UserId, companyId, studentRoleId, startDate: null, CreationSource.RosterImport));

            if (alsoManager)
            {
                _dbContext.UserCompanyRoles.Add(
                    new UserCompanyRole(user.UserId, companyId, managerRoleId, startDate: null, CreationSource.RosterImport));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Detached so a 20,000-row import doesn't accumulate every entity it ever created in the
            // change tracker, which would turn each successive SaveChanges into a longer scan.
            _dbContext.ChangeTracker.Clear();

            return user.UserId;
        });
    }

    public async Task GrantManagerRoleAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        var managerRoleId = await GetRoleIdAsync(Roles.Manager, cancellationToken);

        // UX_UserCompanyRoles_User_Company_Role is unfiltered, so a previously revoked row must be
        // reactivated rather than a second one inserted - see UserCompanyRole.Reactivate.
        var existing = await _dbContext.UserCompanyRoles
            .FirstOrDefaultAsync(
                r => r.UserId == userId && r.CompanyId == companyId && r.RoleId == managerRoleId,
                cancellationToken);

        if (existing is not null)
        {
            existing.Reactivate(startDate: null, CreationSource.RosterImport);
        }
        else
        {
            _dbContext.UserCompanyRoles.Add(
                new UserCompanyRole(userId, companyId, managerRoleId, startDate: null, CreationSource.RosterImport));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();
    }

    public async Task<int> SaveBatchAsync(RosterImportBatch batch, CancellationToken cancellationToken = default)
    {
        _dbContext.RosterImportBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return batch.RosterImportBatchId;
    }

    public async Task<RosterImportBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default) =>
        await _dbContext.RosterImportBatches
            .Include(b => b.Rows)
            .FirstOrDefaultAsync(b => b.RosterImportBatchId == batchId, cancellationToken);

    /// <summary>
    /// The passwords come from Users.PasswordHash, which in this legacy schema holds the value
    /// itself (see AppUser's note on that column) - so the batch never needs its own copy of
    /// anyone's credentials.
    /// </summary>
    public async Task<IReadOnlyList<RosterCreatedUser>> GetCreatedUsersForBatchAsync(
        int batchId, CancellationToken cancellationToken = default)
    {
        // Two plain queries rather than one join. Joining on `row.UserId!.Value` and then filtering
        // the PROJECTED record is not translatable to SQL - EF threw at runtime on the projection
        // filter, which only showed up when the send path was actually exercised.
        var userIds = await _dbContext.RosterImportBatchRows
            .AsNoTracking()
            .Where(r => r.RosterImportBatchId == batchId && r.UserId != null && r.EmployeeCreated)
            .Select(r => r.UserId!.Value)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return [];
        }

        var users = new List<RosterCreatedUser>(userIds.Count);

        // Chunked for the same reason FindExistingUsersByEmailAsync is: a 20,000-row batch would
        // otherwise blow past SQL Server's parameter ceiling.
        foreach (var chunk in userIds.Chunk(EmailLookupChunkSize))
        {
            var found = await _dbContext.Users
                .AsNoTracking()
                .Where(u => chunk.Contains(u.UserId) && u.Email != null)
                .Select(u => new RosterCreatedUser(
                    u.UserId,
                    u.Email!,
                    u.FirstName,
                    u.PasswordHash ?? string.Empty))
                .ToListAsync(cancellationToken);

            users.AddRange(found);
        }

        return users;
    }

    public async Task MarkWelcomeEmailsSentAsync(int batchId, int sentCount, CancellationToken cancellationToken = default)
    {
        var batch = await _dbContext.RosterImportBatches
            .FirstOrDefaultAsync(b => b.RosterImportBatchId == batchId, cancellationToken);

        if (batch is null)
        {
            return;
        }

        batch.MarkWelcomeEmailsSent(sentCount);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CreationSourceStatsDto> GetCreationSourceStatsAsync(
        IReadOnlyCollection<int>? restrictToCompanyIds,
        CancellationToken cancellationToken = default)
    {
        var studentRoleId = await GetRoleIdAsync(Roles.Student, cancellationToken);
        var managerRoleId = await GetRoleIdAsync(Roles.Manager, cancellationToken);

        var query = _dbContext.UserCompanyRoles.AsNoTracking().Where(r => r.IsActive);
        if (restrictToCompanyIds is not null)
        {
            query = query.Where(r => restrictToCompanyIds.Contains(r.CompanyId));
        }

        // Grouped in SQL - this is a whole-table question, so it must not come back row by row.
        var grouped = await query
            .Where(r => r.RoleId == studentRoleId || r.RoleId == managerRoleId)
            .GroupBy(r => new { r.RoleId, r.CreationSource })
            .Select(g => new { g.Key.RoleId, g.Key.CreationSource, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int Count(byte roleId, string source) =>
            grouped.FirstOrDefault(g => g.RoleId == roleId && g.CreationSource == source)?.Count ?? 0;

        return new CreationSourceStatsDto(
            EmployeesManual: Count(studentRoleId, CreationSource.Manual),
            EmployeesRosterImport: Count(studentRoleId, CreationSource.RosterImport),
            ManagersManual: Count(managerRoleId, CreationSource.Manual),
            ManagersRosterImport: Count(managerRoleId, CreationSource.RosterImport),
            EmployeesLegacy: Count(studentRoleId, CreationSource.Legacy),
            ManagersLegacy: Count(managerRoleId, CreationSource.Legacy));
    }

    private async Task<byte> GetRoleIdAsync(string roleName, CancellationToken cancellationToken) =>
        await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.RoleName == roleName)
            .Select(r => r.RoleId)
            .FirstAsync(cancellationToken);
}
