namespace SkillsetsBackend.Application.Skillsoft.Interfaces;

public record SkillsoftProvisionRequest(
    int CompanyId,
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string ManagerEmail,
    string ManagerName);

public record SkillsoftEntitlementRequest(
    int CompanyId,
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string ManagerEmail,
    string ManagerName);

public record SkillsoftProvisionResult(bool Success, string? ErrorMessage);

/// <summary>
/// Best-effort provisioning of a Skillport account for an app user. Calling code decides whether a
/// failure here should block the caller's own operation - this service never throws for a Skillport
/// rejection, only for genuine configuration/programming errors.
/// </summary>
public interface ISkillsoftProvisioningService
{
    /// <summary>Calls Skillport's CreateUserExtended.cfm only - creates the account, does not touch ActiveLibraryCards.</summary>
    Task<SkillsoftProvisionResult> CreateAccountAsync(string username, string password, string firstName, string lastName, CancellationToken cancellationToken = default);

    /// <summary>Writes a fresh 30-day ActiveLibraryCards entitlement row for an account that already exists in Skillport.</summary>
    Task<SkillsoftProvisionResult> RecordEntitlementAsync(SkillsoftEntitlementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Convenience: CreateAccountAsync + RecordEntitlementAsync together, for callers that want both in one step.</summary>
    Task<SkillsoftProvisionResult> ProvisionAsync(SkillsoftProvisionRequest request, CancellationToken cancellationToken = default);
}
