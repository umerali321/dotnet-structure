namespace SkillsetsBackend.Application.Settings.Interfaces;

/// <summary>Encrypts/decrypts a secret (currently just the SMTP password) for storage at rest.
/// Application-layer interface, Infrastructure supplies the real implementation (ASP.NET Core Data
/// Protection) so this layer never depends on a concrete crypto library.</summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedText);
}
