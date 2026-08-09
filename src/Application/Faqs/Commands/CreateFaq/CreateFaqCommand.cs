namespace SkillsetsBackend.Application.Faqs.Commands.CreateFaq;

/// <summary>CompanyId null = a platform-wide/global FAQ. Only SuperAdmin may create one.</summary>
public record CreateFaqCommand(int? CompanyId, string Question, string Answer);
