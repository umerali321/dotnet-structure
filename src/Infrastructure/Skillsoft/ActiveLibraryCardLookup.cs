using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

/// <summary>Batch "does this user currently have an active Skillsoft entitlement" lookup, shared by the Student/Manager list and detail queries so they don't each re-derive the ActiveLibraryCards matching rule.</summary>
public static class ActiveLibraryCardLookup
{
    /// <summary>Returns the set of (CompanyCode, EmailLower) pairs that currently have an active card.</summary>
    public static async Task<HashSet<(string CompanyCode, string EmailLower)>> GetActivePairsAsync(
        ApplicationDbContext dbContext,
        IEnumerable<string> companyCodes,
        CancellationToken cancellationToken)
    {
        var codes = companyCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        if (codes.Count == 0)
        {
            return [];
        }

        var today = DateTime.UtcNow.Date;

        var rows = await dbContext.ActiveLibraryCards.AsNoTracking()
            .Where(c => c.Email != null && codes.Contains(c.CompanyCode) && c.StartDate <= today && c.EndDate >= today)
            .Select(c => new { c.CompanyCode, Email = c.Email!.ToLower() })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.CompanyCode, r.Email)).ToHashSet();
    }
}
