namespace SkillsetsBackend.Infrastructure.Options;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    /// <summary>Gmail account used to authenticate with the SMTP server - never sent to Angular, never logged.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gmail app password - never sent to Angular, never logged.</summary>
    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public string ToAddress { get; set; } = string.Empty;

    public string ToName { get; set; } = string.Empty;
}
