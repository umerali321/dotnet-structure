namespace SkillsetsBackend.Application.SupportContacts.Commands.CreateSupportContact;

/// <summary>CompanyId null = a platform-wide/global contact. Only SuperAdmin may create one.</summary>
public record CreateSupportContactCommand(int? CompanyId, string ContactType, string Value, int SortOrder);
