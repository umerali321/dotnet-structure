namespace SkillsetsBackend.Application.Settings.Commands.SaveSkillportScraperSettings;

public record SaveSkillportScraperSettingsCommand(
    string GroupName, string DateRangeMode, DateOnly? CustomDateFrom, DateOnly? CustomDateTo);
