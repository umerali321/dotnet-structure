using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Options;

namespace SkillsetsBackend.Infrastructure.Auth;

public class SuperAdminAuthenticator : ISuperAdminAuthenticator
{
    private readonly SuperAdminSettings _settings;
    private readonly IPasswordHasher _passwordHasher;

    public SuperAdminAuthenticator(IOptions<SuperAdminSettings> settings, IPasswordHasher passwordHasher)
    {
        _settings = settings.Value;
        _passwordHasher = passwordHasher;
    }

    public SuperAdminIdentity? Validate(string email, string password)
    {
        var account = _settings.Accounts.FirstOrDefault(
            a => string.Equals(email, a.Email, StringComparison.OrdinalIgnoreCase));

        if (account is null || !_passwordHasher.Verify(password, account.PasswordHash))
        {
            return null;
        }

        return new SuperAdminIdentity(account.Id, account.Email, Roles.SuperAdmin);
    }

    public bool IsSuperAdminEmail(string email) =>
        _settings.Accounts.Any(a => string.Equals(email, a.Email, StringComparison.OrdinalIgnoreCase));
}
