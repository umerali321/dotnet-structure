using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.Skillsoft;

/// <summary>Resolves a display identity for the caller, for stamping onto legacy records (ActiveLibraryCards.Manager_ID/FDM_Name).</summary>
public static class CallerIdentityResolver
{
    public static async Task<(string Email, string Name)> ResolveAsync(
        CallerContext caller, IUserDirectory userDirectory, CancellationToken cancellationToken)
    {
        if (caller.IsSuperAdmin)
        {
            return (caller.Email, "SuperAdmin");
        }

        var directoryUser = await userDirectory.FindByIdentifierAsync(caller.Email, cancellationToken);
        var name = directoryUser is null ? null : $"{directoryUser.FirstName} {directoryUser.LastName}".Trim();

        return (caller.Email, string.IsNullOrWhiteSpace(name) ? caller.Email : name);
    }
}
