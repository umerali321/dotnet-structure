namespace SkillsetsBackend.Application.Support.Commands.CreateSupportRequest;

/// <summary>CompanyId is optional when the caller only manages a single company - it is inferred then.</summary>
public record CreateSupportRequestCommand(int? CompanyId, string Subject, string Message);
