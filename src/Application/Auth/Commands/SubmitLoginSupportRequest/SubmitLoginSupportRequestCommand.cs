namespace SkillsetsBackend.Application.Auth.Commands.SubmitLoginSupportRequest;

/// <summary>Submitted from the login page (unauthenticated) when a user can't sign in - emailed
/// directly to support rather than stored, since we may not have any matching account to attach it to.</summary>
public record SubmitLoginSupportRequestCommand(
    string Name,
    string Email,
    string? Phone,
    string? CompanyName,
    string? Message);
