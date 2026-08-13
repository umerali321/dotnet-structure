namespace SkillsetsBackend.Application.Managers.Commands.ProvisionManagerSkillport;

/// <summary>Retries Skillport account creation for a manager that doesn't have one yet (or whose entitlement lapsed). Username is fixed to the manager's existing username - only the company and password are chosen here.</summary>
public record ProvisionManagerSkillportCommand(int UserId, int CompanyId, string Password);
