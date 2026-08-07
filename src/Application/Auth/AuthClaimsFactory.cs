using System.Security.Claims;

namespace SkillsetsBackend.Application.Auth;

public static class AuthClaimsFactory
{
    public static IReadOnlyList<Claim> Create(string userId, string email, string role, int? companyId, string? companyName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("jti", Guid.NewGuid().ToString()),
        };

        if (companyId is not null)
        {
            claims.Add(new Claim(AuthClaimTypes.CompanyId, companyId.Value.ToString()));
            claims.Add(new Claim(AuthClaimTypes.CompanyName, companyName ?? string.Empty));
        }

        return claims;
    }
}
