namespace SkillsetsBackend.Infrastructure.Options;


public class SkillsoftSsoSettings
{
    public const string SectionName = "SkillsoftSso";

    public string IdpEntityId { get; set; } = string.Empty;

    public string SkillsoftAcsUrl { get; set; } = string.Empty;

    public string SkillsoftSpEntityId { get; set; } = string.Empty;

    public string NameIdFormat { get; set; } = "urn:oasis:names:tc:SAML:2.0:nameid-format:unspecified";

    public string FirstNameAttributeName { get; set; } = "FirstName";

    public string LastNameAttributeName { get; set; } = "LastName";

    public string EmailAttributeName { get; set; } = "Email";

    public string? SigningCertificateBase64 { get; set; }

    public string? SigningCertificatePassword { get; set; }

    public string? SigningCertificatePath { get; set; }

    public bool AllowDevSelfSignedCertificate { get; set; }

    public int AssertionValiditySeconds { get; set; } = 300;

    public int LaunchTicketExpirySeconds { get; set; } = 60;
}
