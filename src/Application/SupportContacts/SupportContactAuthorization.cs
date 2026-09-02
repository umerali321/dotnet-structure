using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Application.SupportContacts;

public static class SupportContactAuthorization
{
    /// <summary>SuperAdmin: any contact, including global (CompanyId null). Manager: only their own company's contacts - never global.</summary>
    public static async Task EnsureCanManageContactAsync(CallerContext caller, SupportContact contact, IUserDirectory userDirectory, CancellationToken cancellationToken)
    {
        if (caller.IsPlatformAdmin)
        {
            return;
        }

        if (contact.CompanyId is null)
        {
            throw new UnauthorizedAccessException("You do not have access to this contact.");
        }

        await StudentAuthorization.EnsureCanManageCompanyAsync(caller, contact.CompanyId.Value, userDirectory, cancellationToken);
    }
}
