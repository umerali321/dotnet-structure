namespace SkillsetsBackend.Infrastructure.Options;

public class SkillsoftProvisioningSettings
{
    public const string SectionName = "SkillsoftProvisioning";

    /// <summary>e.g. https://skillsetsonline.skillport.com/skillportfe/bcs/CreateUserExtended.cfm</summary>
    public string CreateUserUrl { get; set; } = string.Empty;

    /// <summary>Skillport admin account used to authorize the CreateUserExtended.cfm call - never sent to Angular, never logged.</summary>
    public string LoginUsername { get; set; } = string.Empty;

    public string LoginPassword { get; set; } = string.Empty;

    /// <summary>Hardcoded to "ss" for now; every company shares one org until per-company org codes are introduced.</summary>
    public string OrgCode { get; set; } = "ss";

    public int RequestTimeoutSeconds { get; set; } = 30;
}
