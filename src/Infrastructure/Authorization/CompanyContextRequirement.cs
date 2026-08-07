using Microsoft.AspNetCore.Authorization;
using SkillsetsBackend.Application.Auth;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Authorization;

/// <summary>
/// Requires an active company to be selected (a "company_id" claim on the token), i.e. the user
/// has completed login/switch-company for a specific company. SuperAdmin always succeeds - it is
/// not scoped to a single company.
/// Apply to any future company-scoped controller/endpoint with [Authorize(Policy = "CompanyContext")].
/// Inject ICurrentCompanyContext to read the active CompanyId for filtering queries, and to check
/// membership in a specific requested company beyond "some company is selected".
/// </summary>
public class CompanyContextRequirement : IAuthorizationRequirement
{
}

public class CompanyContextHandler : AuthorizationHandler<CompanyContextRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CompanyContextRequirement requirement)
    {
        if (context.User.IsInRole(Roles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(c => c.Type == AuthClaimTypes.CompanyId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
