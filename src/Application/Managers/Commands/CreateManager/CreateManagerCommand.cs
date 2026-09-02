namespace SkillsetsBackend.Application.Managers.Commands.CreateManager;

/// <summary>Role is "Manager" or "CompanyAdmin" - a plain Manager caller can only create "Manager"
/// (see CreateManagerCommandHandler); only SuperAdmin/CompanyAdmin may create another CompanyAdmin.</summary>
public record CreateManagerCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Username,
    string Password,
    int CompanyId,
    DateOnly? StartDate,
    string Role = "Manager",
    /// <summary>Also grants this brand-new person an Employee role at the same company, in the
    /// same request - equivalent to creating the Manager then separately calling
    /// AddEmployeeRoleCommand for the same userId.</summary>
    bool AlsoCreateEmployee = false,
    /// <summary>Email this person their sign-in details. Defaults to TRUE because that is what this
    /// endpoint has always done - flipping it to false by default would silently stop welcome emails
    /// for every existing caller. The dialog exposes it so an admin can opt out per account.</summary>
    bool SendWelcomeEmail = true);
