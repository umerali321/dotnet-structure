namespace SkillsetsBackend.Application.Auth.DTOs;

public record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public string TokenType { get; init; } = "Bearer";
}
