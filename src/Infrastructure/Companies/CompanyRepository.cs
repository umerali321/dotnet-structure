using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Companies;

public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CompanyRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CompanyCodeExistsAsync(string companyCode, int? excludeCompanyId = null, CancellationToken cancellationToken = default) =>
        _dbContext.Companies.AnyAsync(c => c.CompanyCode == companyCode && (excludeCompanyId == null || c.CompanyId != excludeCompanyId), cancellationToken);

    public Task<bool> IdentifierInUseAsync(string email, string username, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(u => u.Email == email || u.Username == username, cancellationToken);

    public Task<Company?> GetByIdAsync(int companyId, CancellationToken cancellationToken = default) =>
        _dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId, cancellationToken);

    public async Task<int> CreateCompanyWithAdminAsync(
        Company company,
        AppUser admin,
        CancellationToken cancellationToken = default)
    {
        var companyAdminRoleId = await _dbContext.Roles
            .Where(r => r.RoleName == "CompanyAdmin")
            .Select(r => r.RoleId)
            .FirstAsync(cancellationToken);

        // EnableRetryOnFailure requires operations wrapped this way - a plain BeginTransactionAsync
        // throws because SqlServerRetryingExecutionStrategy doesn't support user-initiated transactions.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // CompanyId/UserId are DB-generated; UserCompanyRole needs both, so the company and the
            // admin user are saved first to obtain them before the dependent row can be constructed.
            _dbContext.Companies.Add(company);
            _dbContext.Users.Add(admin);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var membership = new UserCompanyRole(admin.UserId, company.CompanyId, companyAdminRoleId, startDate: null);
            _dbContext.UserCompanyRoles.Add(membership);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return company.CompanyId;
        });
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
