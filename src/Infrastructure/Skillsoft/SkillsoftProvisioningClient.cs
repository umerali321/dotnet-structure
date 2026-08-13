using System.Security;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Infrastructure.Options;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public record SkillsoftProvisionApiResult(bool Success, string? ErrorMessage);

/// <summary>
/// Calls the legacy Skillport CreateUserExtended.cfm form-post API to provision a new Skillport
/// account. This is always HTTP 200 regardless of outcome - success/failure is only signalled by
/// the "success" attribute on the returned XML &lt;result&gt; element, e.g.:
/// &lt;result success="0"&gt;&lt;errors&gt;&lt;parameter-invalid&gt;&lt;message id="...">Error: ...&lt;/message&gt;&lt;/parameter-invalid&gt;&lt;/errors&gt;&lt;/result&gt;
/// </summary>
public class SkillsoftProvisioningClient
{
    private readonly HttpClient _httpClient;
    private readonly SkillsoftProvisioningSettings _settings;

    public SkillsoftProvisioningClient(HttpClient httpClient, IOptions<SkillsoftProvisioningSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<SkillsoftProvisionApiResult> CreateUserAsync(
        string username, string password, string firstName, string lastName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.CreateUserUrl))
        {
            throw new InvalidOperationException("Skillsoft provisioning is not configured. Set SkillsoftProvisioning:CreateUserUrl.");
        }

        var profileFieldValues =
            "<ProfileFieldValues>" +
            $"<FieldValue id=\"_sys_firstname\"><Value>{SecurityElement.Escape(firstName)}</Value></FieldValue>" +
            $"<FieldValue id=\"_sys_lastname\"><Value>{SecurityElement.Escape(lastName)}</Value></FieldValue>" +
            "</ProfileFieldValues>";

        var form = new Dictionary<string, string>
        {
            ["loginUsername"] = _settings.LoginUsername,
            ["loginPassword"] = _settings.LoginPassword,
            ["restype"] = "2",
            ["username"] = username,
            ["password"] = password,
            ["orgCode"] = _settings.OrgCode,
            ["btnSubmit"] = "Submit",
            ["ProfileFieldValues"] = profileFieldValues,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.RequestTimeoutSeconds)));

        using var response = await _httpClient.PostAsync(_settings.CreateUserUrl, new FormUrlEncodedContent(form), cts.Token);
        var body = await response.Content.ReadAsStringAsync(cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            return new SkillsoftProvisionApiResult(false, $"Skillport returned HTTP {(int)response.StatusCode}.");
        }

        XElement root;
        try
        {
            root = XDocument.Parse(body).Root ?? throw new InvalidOperationException("Empty XML document.");
        }
        catch (Exception)
        {
            return new SkillsoftProvisionApiResult(false, "Skillport returned an unexpected (non-XML) response.");
        }

        var success = root.Attribute("success")?.Value;
        if (success == "1")
        {
            return new SkillsoftProvisionApiResult(true, null);
        }

        return new SkillsoftProvisionApiResult(false, ExtractErrorMessage(root, body));
    }

    /// <summary>
    /// Skillport's error XML shape isn't fully documented - try the known "message" element first
    /// (e.g. &lt;errors&gt;&lt;parameter-invalid&gt;&lt;message&gt;...), then fall back to any text inside
    /// &lt;errors&gt;, then the raw body itself, so an unfamiliar shape still surfaces something
    /// diagnosable instead of a generic "rejected" message.
    /// </summary>
    private static string ExtractErrorMessage(XElement root, string rawBody)
    {
        var message = root.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, "message", StringComparison.OrdinalIgnoreCase))?.Value.Trim();
        if (!string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        var errorsElement = root.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, "errors", StringComparison.OrdinalIgnoreCase));
        var errorsText = errorsElement?.Value.Trim();
        if (!string.IsNullOrWhiteSpace(errorsText))
        {
            return errorsText;
        }

        var snippet = rawBody.Length > 300 ? rawBody[..300] + "..." : rawBody;
        return $"Skillport rejected the request: {snippet}";
    }
}
