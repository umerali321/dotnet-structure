namespace SkillsetsBackend.Application.Students.Commands.ProvisionStudentSkillport;

/// <summary>Retries Skillport account creation for a student that doesn't have one yet (or whose entitlement lapsed). Username is fixed to the student's existing username - only the company and password are chosen here.</summary>
public record ProvisionStudentSkillportCommand(int UserId, int CompanyId, string Password);
