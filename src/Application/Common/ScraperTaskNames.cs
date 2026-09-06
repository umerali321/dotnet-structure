namespace SkillsetsBackend.Application.Common;

/// <summary>Must exactly match the names register_nightly_task.ps1/register_course_scraper_task.ps1
/// register the corresponding Windows Scheduled Tasks under on the server.</summary>
public static class ScraperTaskNames
{
    public const string NightlyTranscriptSync = "SkillSets - Nightly Learning Transcript Sync";
    public const string CourseLibraryScraper = "SkillSets - Course Library Scraper";
}
