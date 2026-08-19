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
    bool CreateInSkillport = false,
    string Role = "Manager");
