namespace SkillsetsBackend.Infrastructure.Skillsoft.Olsa;

/// <summary>
/// One asset (course/title) as returned by a Search &amp; Learn operation or SL_GetAssetDetail.
/// <see cref="Fields"/> carries every leaf element OLSA returned for the asset, by local name -
/// kept alongside the best-guess named properties below since the OLSA Integration Guide never
/// gives the exact metadata element names ("see the OLSA WSDL for complete details" throughout),
/// so nothing observed on the wire is silently dropped.
/// </summary>
public sealed record OlsaAsset(
    string AssetId,
    string? Title,
    string? AssetType,
    string? BinName,
    string? LanguageCode,
    IReadOnlyDictionary<string, string?> Fields);

public sealed record OlsaSearchPage(
    string? SearchId,
    IReadOnlyList<OlsaAsset> Assets,
    bool HasMore);

/// <summary>One usage/completion record from UD_GetAssetResults.</summary>
public sealed record OlsaUsageResult(
    string? AssetId,
    string? Title,
    string? CompletionStatus,
    string? FirstAccessDate,
    string? LastAccessDate,
    string? CompletionDate,
    string? Score,
    IReadOnlyDictionary<string, string?> Fields);

public sealed class OlsaFaultException : Exception
{
    public string? FaultCode { get; }

    public OlsaFaultException(string? faultCode, string message)
        : base(message)
    {
        FaultCode = faultCode;
    }
}
