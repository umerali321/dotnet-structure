using Microsoft.AspNetCore.Identity;
using SkillsetsBackend.Application.Auth.Interfaces;

namespace SkillsetsBackend.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    private static readonly object HashSubject = new();

    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashSubject, password);

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(HashSubject, hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
