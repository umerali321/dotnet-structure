using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Domain.Skillsoft;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

/// <summary>Resolves the caller's Skillsoft entitlement (username/password on ActiveLibraryCards) for a company. Shared by every Skillsoft-facing service so the lookup lives in one place.</summary>
public class ActiveLibraryCardResolver
{
    private readonly ApplicationDbContext _dbContext;

    public ActiveLibraryCardResolver(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ActiveLibraryCard> ResolveAsync(int userId, int companyId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (user?.Email is null)
        {
            throw new UnauthorizedAccessException("Your account has no email on file - Skillsoft access requires one.");
        }

        var companyCode = await _dbContext.Companies.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .Select(c => c.CompanyCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(companyCode))
        {
            throw new UnauthorizedAccessException("Company not found.");
        }

        var today = DateTime.UtcNow.Date;

        var card = await _dbContext.ActiveLibraryCards.AsNoTracking()
            .Where(c => c.CompanyCode == companyCode
                && c.Email != null && c.Email.ToLower() == user.Email.ToLower()
                && c.StartDate <= today
                && c.EndDate >= today)
            .OrderByDescending(c => c.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
        {
            throw new UnauthorizedAccessException("You do not have an active Skillsoft library card for this company.");
        }

        return card;
    }
}
