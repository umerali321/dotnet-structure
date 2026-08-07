using System.Security.Cryptography;
using System.Text;
using SkillsetsBackend.Application.Auth.Interfaces;

namespace SkillsetsBackend.Infrastructure.Auth;

/// <summary>
/// Compares directly against the existing legacy credential value (a short plaintext PIN/password,
/// not a hash - see AGENTS.md). Uses a fixed-time comparison to avoid a timing side-channel, but
/// does not attempt to verify a cryptographic hash because the existing data isn't one.
/// </summary>
public class LegacyCredentialVerifier : ILegacyCredentialVerifier
{
    public bool Verify(string submittedPassword, string storedValue)
    {
        var submittedBytes = Encoding.UTF8.GetBytes(submittedPassword);
        var storedBytes = Encoding.UTF8.GetBytes(storedValue);

        if (submittedBytes.Length != storedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(submittedBytes, storedBytes);
    }
}
