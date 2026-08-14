using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Skillsoft;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

/// <summary>Validates the caller has an active role at the company and resolves their ActiveLibraryCard. Shared by every Skillsoft-facing service.</summary>
public class SkillsoftAccessGuard
{
    private readonly IUserDirectory _userDirectory;
    private readonly ActiveLibraryCardResolver _cardResolver;

    public SkillsoftAccessGuard(IUserDirectory userDirectory, ActiveLibraryCardResolver cardResolver)
    {
        _userDirectory = userDirectory;
        _cardResolver = cardResolver;
    }

    public async Task<ActiveLibraryCard> ResolveForCallerAsync(CallerContext caller, int companyId, CancellationToken cancellationToken)
    {
        var card = await TryResolveForCallerAsync(caller, companyId, cancellationToken);

        if (card is null)
        {
            throw new UnauthorizedAccessException("You do not have an active Skillsoft library card for this company.");
        }

        return card;
    }

    /// <summary>Same role-check + card lookup as ResolveForCallerAsync, but returns null instead of throwing when there's simply no active card yet.</summary>
    public async Task<ActiveLibraryCard?> TryResolveForCallerAsync(CallerContext caller, int companyId, CancellationToken cancellationToken)
    {
        var userId = await EnsureActiveRoleAsync(caller, companyId, cancellationToken);

        return await _cardResolver.TryResolveAsync(userId, companyId, cancellationToken);
    }

    /// <summary>Confirms the caller has an active role at the company and returns their DB user id - shared by every method here that needs that check before touching Skillsoft data.</summary>
    public async Task<int> EnsureActiveRoleAsync(CallerContext caller, int companyId, CancellationToken cancellationToken)
    {
        var userId = caller.DbUserId ?? throw new UnauthorizedAccessException("Not authenticated.");

        var activeCompanyRoles = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);
        if (!activeCompanyRoles.Any(r => r.CompanyId == companyId))
        {
            throw new UnauthorizedAccessException("You do not have an active role at that company.");
        }

        return userId;
    }
}
