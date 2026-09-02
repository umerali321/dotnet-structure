using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Common;

/// <summary>SuperAdmin has no real Users row (config-based identity - see AGENTS.md), so it cannot
/// directly author a SkillTrax/Assignment. Instead, per product decision, SuperAdmin selects a real
/// Manager or Company Admin at the target company to act on behalf of - the resulting record is
/// then indistinguishable from one that Manager created themselves (shows up on their own side
/// exactly as if they made it), while still requiring the action to resolve to a real, currently
/// active company member rather than an arbitrary id.</summary>
public static class ActingAsResolver
{
    public static async Task<int> ResolveCreatorUserIdAsync(
        CallerContext caller, int? actingAsUserId, int companyId, IUserDirectory userDirectory, CancellationToken cancellationToken)
    {
        if (!caller.IsPlatformAdmin)
        {
            return caller.DbUserId ?? throw new UnauthorizedAccessException("Only a Manager or Company Admin account can perform this action.");
        }

        if (actingAsUserId is not int userId)
        {
            throw new AppValidationException([new ValidationFailure("ActingAsUserId", "Select a Manager or Company Admin to create this on behalf of.")]);
        }

        var roles = await userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);
        var isValid = roles.Any(r => r.CompanyId == companyId && (Roles.Normalize(r.RoleName) == Roles.Manager || r.RoleName == Roles.CompanyAdmin));
        if (!isValid)
        {
            throw new AppValidationException([new ValidationFailure("ActingAsUserId", "That user is not an active Manager or Company Admin at the selected company.")]);
        }

        return userId;
    }
}
