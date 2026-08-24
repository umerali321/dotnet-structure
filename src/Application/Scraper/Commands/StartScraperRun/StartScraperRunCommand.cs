namespace SkillsetsBackend.Application.Scraper.Commands.StartScraperRun;

public record StartScraperRunCommand(IReadOnlyList<string> Categories, string Mode, int? Limit);
