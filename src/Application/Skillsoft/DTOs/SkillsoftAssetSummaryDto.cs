namespace SkillsetsBackend.Application.Skillsoft.DTOs;

public record SkillsoftAssetSummaryDto(
    string AssetId,
    string? Title,
    string? AssetType,
    string? BinName,
    string? LanguageCode);
