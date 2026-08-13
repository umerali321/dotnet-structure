namespace SkillsetsBackend.Application.Managers.Commands.CreateManager;

public record CreateManagerCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Username,
    string Password,
    int CompanyId,
    DateOnly? StartDate,
    bool CreateInSkillport = false);
