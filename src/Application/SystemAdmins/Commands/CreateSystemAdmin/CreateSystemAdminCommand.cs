namespace SkillsetsBackend.Application.SystemAdmins.Commands.CreateSystemAdmin;

/// <summary>
/// A System Administrator belongs to NO company - they administer all of them. There is deliberately
/// no CompanyId here and none is asked for when creating one.
///
/// Internally the account still gets a UserCompanyRoles row, because that is the only way any
/// DB-backed account resolves a role at login. The handler picks that row itself and it is never
/// used to scope anything (see CallerContext.HasGlobalCompanyScope) - it is bookkeeping, not
/// membership, and nothing in the UI presents it as the person's company.
/// </summary>
/// <remarks>No Username: for these accounts it only ever duplicated the email, so the email is used
/// as the username too rather than asking for the same value twice.</remarks>
public record CreateSystemAdminCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Password);
