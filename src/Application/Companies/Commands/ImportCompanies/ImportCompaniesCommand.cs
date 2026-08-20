using SkillsetsBackend.Application.Companies.Interfaces;

namespace SkillsetsBackend.Application.Companies.Commands.ImportCompanies;

public record ImportCompaniesCommand(IReadOnlyList<ImportRawRow> Rows);
