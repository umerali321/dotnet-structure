using Microsoft.AspNetCore.DataProtection;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Infrastructure.Security;

/// <summary>Wraps ASP.NET Core's Data Protection API - the framework's standard, key-managed way to
/// encrypt a secret for storage at rest (key generation/rotation handled by the framework, not
/// hand-rolled here). The purpose string scopes the key ring to this exact use, so a protector
/// created for a different purpose elsewhere in the app can never decrypt an SMTP password.</summary>
public class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "SkillsetsBackend.Settings.SmtpPassword.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
