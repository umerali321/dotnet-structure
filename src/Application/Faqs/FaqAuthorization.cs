using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Application.Faqs;

public static class FaqAuthorization
{
    /// <summary>SuperAdmin: any FAQ, including global (CompanyId null). Manager: only their own company's FAQs - never global.</summary>
    public static async Task EnsureCanManageFaqAsync(CallerContext caller, Faq faq, IUserDirectory userDirectory, CancellationToken cancellationToken)
    {
        if (caller.IsSuperAdmin)
        {
            return;
        }

        if (faq.CompanyId is null)
        {
            throw new UnauthorizedAccessException("You do not have access to this FAQ.");
        }

        await StudentAuthorization.EnsureCanManageCompanyAsync(caller, faq.CompanyId.Value, userDirectory, cancellationToken);
    }
}
