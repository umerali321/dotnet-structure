namespace SkillsetsBackend.Application.Settings.DTOs;

public record SkillportScraperSettingsDto(
    int SkillportScraperSettingsId,
    string GroupName,
    string DateRangeMode,
    DateOnly? CustomDateFrom,
    DateOnly? CustomDateTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
