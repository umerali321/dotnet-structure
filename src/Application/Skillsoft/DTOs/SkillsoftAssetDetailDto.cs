namespace SkillsetsBackend.Application.Skillsoft.DTOs;

public record SkillsoftAssetDetailDto(
    string AssetId,
    string? Title,
    string? AssetType,
    string? LanguageCode,
    IReadOnlyDictionary<string, string?> Metadata);
