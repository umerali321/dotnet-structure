namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface ISuperAdminAuthenticator
{
    SuperAdminIdentity? Validate(string email, string password);

    /// <summary>True if `email` belongs to a configured SuperAdmin account. SuperAdmin is
    /// config-based, not a `Users` row (see AGENTS.md), so the normal "is this email already taken"
    /// duplicate check against the Users table can never see it - without this, a Manager/Student's
    /// Email could be silently changed to match a SuperAdmin's, and since LoginCommandHandler checks
    /// SuperAdmin before the database, that account would never be able to log in with its own
    /// email again.</summary>
    bool IsSuperAdminEmail(string email);
}

public record SuperAdminIdentity(Guid Id, string Email, string Role);
