namespace SkillsetsBackend.Infrastructure.Options;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; set; } = 30;

    public int RefreshTokenExpiryDays { get; set; } = 7;
}
