namespace SkillsetsBackend.Infrastructure.Options;

public class SkillsoftOlsaSettings
{
    public const string SectionName = "SkillsoftOlsa";

    /// <summary>The OLSA SOAP endpoint URL for this tenant (from the OLSA WSDL's soap:address).</summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>The customer-context identifier OLSA associates with this tenant's credentials.</summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// The OLSA WSDL's target XML namespace (from its &lt;definitions targetNamespace="..."&gt;).
    /// The OLSA Integration Guide doesn't state this value - read it from the tenant's actual WSDL
    /// (usually reachable at EndpointUrl + "?wsdl") and set it here before going live.
    /// </summary>
    public string TargetNamespace { get; set; } = string.Empty;

    /// <summary>
    /// The WS-Security UsernameToken password ("Shared Secret" in Skillsoft's terminology), issued by
    /// Skillsoft alongside CustomerId. Per Skillsoft's own SoapUI setup guide the WSS Username field
    /// is the CustomerId itself (not a separate value) and the password must be sent as PasswordDigest,
    /// not PasswordText - see OlsaSoapClient.BuildEnvelope.
    /// </summary>
    public string WssPassword { get; set; } = string.Empty;

    public int RequestTimeoutSeconds { get; set; } = 30;
}
