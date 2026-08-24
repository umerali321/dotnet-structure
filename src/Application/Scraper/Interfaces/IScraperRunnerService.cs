using SkillsetsBackend.Application.Scraper.Models;

namespace SkillsetsBackend.Application.Scraper.Interfaces;

/// <summary>Tracks exactly one scraper run at a time (there is only ever one Skillport/Selenium
/// session to worry about). Implemented as an in-memory singleton - see ScraperRunnerService for
/// the accepted limitation that a run doesn't survive an API restart.</summary>
public interface IScraperRunnerService
{
    ScraperRunSnapshot GetSnapshot();

    /// <summary>Throws ConflictException if a run is already in progress.</summary>
    ScraperRunSnapshot StartRun(IReadOnlyList<string> categories, string mode, int? limit, string startedByEmail);

    /// <summary>Throws ConflictException if no run is currently in progress.</summary>
    ScraperRunSnapshot StopRun();
}
