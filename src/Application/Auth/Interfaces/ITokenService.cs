using System.Security.Claims;

namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(IEnumerable<Claim> claims);

    (string Token, DateTimeOffset ExpiresAt) GenerateRefreshToken();
}
