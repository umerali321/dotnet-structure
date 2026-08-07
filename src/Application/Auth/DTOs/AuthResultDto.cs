namespace SkillsetsBackend.Application.Auth.DTOs;

public record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    string Role,
    CompanyDto? CurrentCompany,
    IReadOnlyList<CompanyDto> Companies)
{
    public string TokenType { get; init; } = "Bearer";
}
