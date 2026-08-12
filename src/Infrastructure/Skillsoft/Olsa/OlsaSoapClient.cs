using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Infrastructure.Options;

namespace SkillsetsBackend.Infrastructure.Skillsoft.Olsa;

/// <summary>
/// Raw SOAP 1.2 client for Skillsoft's OLSA web services (Search &amp; Learn, Usage Data
/// Synchronization, SignOn). Hand-rolled instead of a generated WCF/svcutil client because
/// WS-Security UsernameToken isn't cleanly supported by .NET's System.ServiceModel without extra
/// machinery, and this needs no new package beyond HttpClient + System.Xml.Linq.
///
/// Auth per Skillsoft's own SoapUI setup guide (support.skillsoft.com/support/selfhelp/misc/soapui_config.htm):
/// the WSS UsernameToken's Username is the CustomerId itself (there's no separate API username), the
/// Password must be sent as PasswordDigest (not PasswordText), and a WS-Security Timestamp accompanies
/// it. The real endpoint follows the pattern "http://&lt;tenant&gt;.skillwsa.com/olsa/services/Olsa?wsdl"
/// (from Skillsoft's OLSA Client Toolkit Readme) - set the tenant-specific value in
/// SkillsoftOlsa:EndpointUrl.
///
/// IMPORTANT - remaining wire-format caveat: the OLSA Integration Guide describes every operation's
/// inputs and outputs only in prose ("see the OLSA WSDL for complete details") and never states the
/// actual request/response element names or target XML namespace. The envelope below uses the
/// operation's documented argument names verbatim as element names (those ARE given literally in the
/// guide, e.g. "customerId", "searchPhrase", "userName") and treats the operation name itself as the
/// SOAP body's wrapper element (the near-universal "document/literal wrapped" convention for a WSDL of
/// this era). Response parsing below is deliberately defensive (matches by local name only, ignores
/// the response namespace, and keeps every leaf field it saw rather than a rigid fixed shape) so it
/// degrades gracefully instead of silently misparsing. This MUST be verified against the tenant's real
/// OLSA WSDL and a live response before it's trusted in production. Set SkillsoftOlsa:TargetNamespace
/// once that WSDL is available.
/// </summary>
public sealed class OlsaSoapClient
{
    private static readonly XNamespace Soap12 = "http://www.w3.org/2003/05/soap-envelope";
    private static readonly XNamespace Wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace Wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string PasswordDigestType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest";
    private const string Base64BinaryEncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";
    private static readonly TimeSpan TimestampValidity = TimeSpan.FromSeconds(60);

    private readonly HttpClient _httpClient;
    private readonly SkillsoftOlsaSettings _settings;

    public OlsaSoapClient(HttpClient httpClient, IOptions<SkillsoftOlsaSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<OlsaSearchPage> FederatedSearchAsync(
        string customerId, string searchPhrase, string userName, string languageCode = "en", CancellationToken cancellationToken = default)
    {
        var response = await CallAsync(
            "SL_FederatedSearch",
            [
                ("customerId", customerId),
                ("searchPhrase", searchPhrase),
                ("languageCode", languageCode),
                ("userName", userName),
                ("enable508", "false"),
            ],
            cancellationToken);

        return ParseSearchPage(response);
    }

    public async Task<OlsaSearchPage> PaginateSearchAsync(
        string customerId, string searchId, string? binName, int start, int count, CancellationToken cancellationToken = default)
    {
        var response = await CallAsync(
            "SL_PaginateSearch",
            [
                ("customerId", customerId),
                ("searchId", searchId),
                ("binName", binName ?? string.Empty),
                ("start", start.ToString()),
                ("count", count.ToString()),
                ("enable508", "false"),
            ],
            cancellationToken);

        return ParseSearchPage(response) with { SearchId = searchId };
    }

    public async Task<OlsaAsset?> GetAssetDetailAsync(
        string customerId, string assetId, string userName, CancellationToken cancellationToken = default)
    {
        var response = await CallAsync(
            "SL_GetAssetDetail",
            [
                ("customerId", customerId),
                ("assetId", assetId),
                ("username", userName),
            ],
            cancellationToken);

        return ParseAsset(response);
    }

    public async Task<IReadOnlyList<OlsaUsageResult>> GetAssetResultsAsync(
        string customerId, string userName, string? assetId, bool summaryLevel, CancellationToken cancellationToken = default)
    {
        var parameters = new List<(string, string?)>
        {
            ("customerId", customerId),
            ("userName", userName),
            ("summaryLevel", summaryLevel ? "true" : "false"),
        };
        if (!string.IsNullOrWhiteSpace(assetId))
        {
            parameters.Add(("assetId", assetId));
        }

        XElement response;
        try
        {
            response = await CallAsync("UD_GetAssetResults", parameters, cancellationToken);
        }
        catch (OlsaFaultException ex) when (ex.FaultCode?.Contains("NoResultsAvailable", StringComparison.OrdinalIgnoreCase) == true)
        {
            return [];
        }

        return ParseUsageResults(response);
    }

    public async Task<string> GetMultiActionSignOnUrlAsync(
        string customerId,
        string actionType,
        string? assetId,
        string userName,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<(string, string?)>
        {
            ("customerId", customerId),
            ("actionType", actionType),
            ("newUserName", userName),
            ("username", userName),
            ("password", password),
            ("firstname", firstName),
            ("lastname", lastName),
            ("enable508", "false"),
        };
        if (!string.IsNullOrWhiteSpace(assetId))
        {
            parameters.Add(("assetId", assetId));
        }

        var response = await CallAsync("SO_GetMultiActionOnSignOnURL", parameters, cancellationToken);

        var url = response.DescendantsAndSelf()
            .Where(e => !e.HasElements && !string.IsNullOrWhiteSpace(e.Value))
            .Select(e => e.Value.Trim())
            .FirstOrDefault(v => v.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || v.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("OLSA SO_GetMultiActionOnSignOnURL did not return a launch URL.");
        }

        return url;
    }

    private async Task<XElement> CallAsync(string operation, IEnumerable<(string Name, string? Value)> parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.EndpointUrl))
        {
            throw new InvalidOperationException("Skillsoft OLSA is not configured. Set SkillsoftOlsa:EndpointUrl.");
        }

        var envelope = BuildEnvelope(operation, parameters);

        using var content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml")
        {
            CharSet = "utf-8",
        };
        content.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("action", $"\"{operation}\""));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.RequestTimeoutSeconds)));

        using var response = await _httpClient.PostAsync(_settings.EndpointUrl, content, cts.Token);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        XDocument doc;
        try
        {
            doc = XDocument.Parse(responseBody);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"OLSA {operation} returned a non-XML response (HTTP {(int)response.StatusCode}).", ex);
        }

        var body = doc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Body");
        var fault = body?.Elements().FirstOrDefault(e => e.Name.LocalName == "Fault");
        if (fault is not null)
        {
            var faultCode = fault.Descendants().FirstOrDefault(e => e.Name.LocalName is "Value" or "faultcode")?.Value;
            var faultText = fault.Descendants().FirstOrDefault(e => e.Name.LocalName is "Text" or "faultstring")?.Value
                ?? "OLSA returned a SOAP fault with no message.";
            throw new OlsaFaultException(faultCode, $"OLSA {operation} failed: {faultText}");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OLSA {operation} returned HTTP {(int)response.StatusCode}.");
        }

        var result = body?.Elements().FirstOrDefault();
        if (result is null)
        {
            throw new InvalidOperationException($"OLSA {operation} returned an empty SOAP body.");
        }

        return result;
    }

    private XDocument BuildEnvelope(string operation, IEnumerable<(string Name, string? Value)> parameters)
    {
        XNamespace ns = string.IsNullOrWhiteSpace(_settings.TargetNamespace)
            ? XNamespace.None
            : _settings.TargetNamespace;

        var operationElement = new XElement(ns + operation,
            parameters.Where(p => p.Value is not null).Select(p => new XElement(ns + p.Name, p.Value)));

        var now = DateTime.UtcNow;
        var created = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var expires = (now + TimestampValidity).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var nonceBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var passwordDigest = ComputePasswordDigest(nonceBytes, created, _settings.WssPassword);

        var securityHeader = new XElement(Wsse + "Security",
            new XAttribute(XNamespace.Xmlns + "wsse", Wsse.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "wsu", Wsu.NamespaceName),
            new XElement(Wsu + "Timestamp",
                new XElement(Wsu + "Created", created),
                new XElement(Wsu + "Expires", expires)),
            new XElement(Wsse + "UsernameToken",
                // Per Skillsoft's own SoapUI setup guide, the WSS Username IS the CustomerId - there
                // is no separate API username, and the password must be PasswordDigest, not PasswordText.
                new XElement(Wsse + "Username", _settings.CustomerId),
                new XElement(Wsse + "Password", new XAttribute("Type", PasswordDigestType), passwordDigest),
                new XElement(Wsse + "Nonce", new XAttribute("EncodingType", Base64BinaryEncodingType), Convert.ToBase64String(nonceBytes)),
                new XElement(Wsu + "Created", created)));

        return new XDocument(
            new XElement(Soap12 + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", Soap12.NamespaceName),
                new XElement(Soap12 + "Header", securityHeader),
                new XElement(Soap12 + "Body", operationElement)));
    }

    /// <summary>WSS UsernameToken PasswordDigest = Base64(SHA1(nonce + created + password)), per the OASIS Username Token Profile 1.0.</summary>
    private static string ComputePasswordDigest(byte[] nonceBytes, string created, string password)
    {
        var createdBytes = System.Text.Encoding.UTF8.GetBytes(created);
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

        var combined = new byte[nonceBytes.Length + createdBytes.Length + passwordBytes.Length];
        Buffer.BlockCopy(nonceBytes, 0, combined, 0, nonceBytes.Length);
        Buffer.BlockCopy(createdBytes, 0, combined, nonceBytes.Length, createdBytes.Length);
        Buffer.BlockCopy(passwordBytes, 0, combined, nonceBytes.Length + createdBytes.Length, passwordBytes.Length);

        var hash = System.Security.Cryptography.SHA1.HashData(combined);
        return Convert.ToBase64String(hash);
    }

    private static OlsaSearchPage ParseSearchPage(XElement response)
    {
        var searchId = FindFirstValue(response, "searchId", "searchid");

        var bins = response.Descendants().Where(e => e.Name.LocalName.Equals("bin", StringComparison.OrdinalIgnoreCase)).ToList();
        var assetElements = bins.Count > 0
            ? bins.SelectMany(bin =>
            {
                var binName = FindFirstValue(bin, "binName", "name") ?? bin.Attribute("name")?.Value;
                return bin.Descendants().Where(e => e.Name.LocalName.Equals("asset", StringComparison.OrdinalIgnoreCase))
                    .Select(assetElement => (Element: assetElement, BinName: binName));
            }).ToList()
            : response.Descendants().Where(e => e.Name.LocalName.Equals("asset", StringComparison.OrdinalIgnoreCase))
                .Select(e => (Element: e, BinName: (string?)null)).ToList();

        var assets = assetElements
            .Select(x => ParseAsset(x.Element, x.BinName))
            .Where(a => a is not null)
            .Select(a => a!)
            .ToList();

        var hasMore = assets.Count > 0;

        return new OlsaSearchPage(searchId, assets, hasMore);
    }

    private static OlsaAsset? ParseAsset(XElement element, string? binName = null)
    {
        var fields = Flatten(element);

        var assetId = FindFirstValue(element, "assetId", "id") ?? element.Attribute("id")?.Value;
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return null;
        }

        return new OlsaAsset(
            AssetId: assetId,
            Title: FindFirstValue(element, "title", "name", "assetTitle"),
            AssetType: FindFirstValue(element, "assetType", "type"),
            BinName: binName ?? FindFirstValue(element, "binName"),
            LanguageCode: FindFirstValue(element, "languageCode", "language"),
            Fields: fields);
    }

    private static List<OlsaUsageResult> ParseUsageResults(XElement response)
    {
        var resultElements = response.Descendants()
            .Where(e => e.Name.LocalName is "result" or "assetResult" or "usageResult" or "resultElement")
            .ToList();

        if (resultElements.Count == 0)
        {
            resultElements = response.Elements().ToList();
        }

        return resultElements.Select(e =>
        {
            var fields = Flatten(e);
            return new OlsaUsageResult(
                AssetId: FindFirstValue(e, "assetId", "id"),
                Title: FindFirstValue(e, "title", "assetTitle", "name"),
                CompletionStatus: FindFirstValue(e, "completionStatus", "status"),
                FirstAccessDate: FindFirstValue(e, "firstAccessDate", "firstAccess"),
                LastAccessDate: FindFirstValue(e, "lastAccessDate", "lastAccess"),
                CompletionDate: FindFirstValue(e, "completionDate"),
                Score: FindFirstValue(e, "score", "highScore"),
                Fields: fields);
        }).ToList();
    }

    private static string? FindFirstValue(XElement root, params string[] localNames)
    {
        foreach (var name in localNames)
        {
            var match = root.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase) && !e.HasElements);
            if (match is not null)
            {
                return match.Value;
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string?> Flatten(XElement element)
    {
        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var leaf in element.Descendants().Where(e => !e.HasElements))
        {
            fields.TryAdd(leaf.Name.LocalName, leaf.Value);
        }

        return fields;
    }
}
