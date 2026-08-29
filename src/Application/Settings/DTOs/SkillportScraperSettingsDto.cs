namespace SkillsetsBackend.Application.Settings.DTOs;

public record SkillportScraperSettingsDto(
    int SkillportScraperSettingsId,
    string GroupName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
