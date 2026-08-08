namespace SkillsetsBackend.Application.Managers.Commands.UpdateManager;

public record UpdateManagerCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Username);
