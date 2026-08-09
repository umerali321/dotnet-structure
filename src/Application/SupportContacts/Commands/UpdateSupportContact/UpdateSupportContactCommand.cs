namespace SkillsetsBackend.Application.SupportContacts.Commands.UpdateSupportContact;

public record UpdateSupportContactCommand(string ContactType, string Value, int SortOrder);
