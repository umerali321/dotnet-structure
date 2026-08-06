namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface ISuperAdminAuthenticator
{
    SuperAdminIdentity? Validate(string email, string password);
}

public record SuperAdminIdentity(Guid Id, string Email, string Role);
