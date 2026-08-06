using System.Security.Claims;

namespace SkillsetsBackend.Application.Auth;

public static class AuthClaimsFactory
{
    public static IReadOnlyList<Claim> Create(Guid userId, string email, string role) =>
    [
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, role),
        new Claim("jti", Guid.NewGuid().ToString()),
    ];
}
