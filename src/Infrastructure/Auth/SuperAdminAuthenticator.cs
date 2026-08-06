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
        if (!string.Equals(email, _settings.Email, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!_passwordHasher.Verify(password, _settings.PasswordHash))
        {
            return null;
        }

        return new SuperAdminIdentity(_settings.Id, _settings.Email, Roles.SuperAdmin);
    }
}
