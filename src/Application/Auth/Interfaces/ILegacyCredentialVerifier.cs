namespace SkillsetsBackend.Application.Auth.Interfaces;

/// <summary>
/// Verifies a submitted password against the legacy credential value stored in the existing
/// database (Users.PasswordHash / UserCredentials.PasswordHash). The existing values are short
/// plaintext PINs/passwords, not cryptographic hashes - do not "fix" this to call a real hash
/// verifier, it will simply never match real data. See AGENTS.md for the full finding.
/// </summary>
public interface ILegacyCredentialVerifier
{
    bool Verify(string submittedPassword, string storedValue);
}
