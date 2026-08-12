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
        var userId = caller.DbUserId ?? throw new UnauthorizedAccessException("Not authenticated.");

        var activeCompanyRoles = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);
        if (!activeCompanyRoles.Any(r => r.CompanyId == companyId))
        {
            throw new UnauthorizedAccessException("You do not have an active role at that company.");
        }

        return await _cardResolver.ResolveAsync(userId, companyId, cancellationToken);
    }
}
