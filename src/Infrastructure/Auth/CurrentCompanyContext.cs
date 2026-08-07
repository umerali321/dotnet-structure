using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SkillsetsBackend.Application.Auth;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Auth;

public class CurrentCompanyContext : ICurrentCompanyContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentCompanyContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? CompanyId
    {
        get
        {
            var value = User?.FindFirst(AuthClaimTypes.CompanyId)?.Value;
            return int.TryParse(value, out var companyId) ? companyId : null;
        }
    }

    public string? CompanyName => User?.FindFirst(AuthClaimTypes.CompanyName)?.Value;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsSuperAdmin => Role == Roles.SuperAdmin;
}
