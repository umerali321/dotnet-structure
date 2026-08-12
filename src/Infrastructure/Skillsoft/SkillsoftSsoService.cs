using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Infrastructure.Options;
using SkillsetsBackend.Infrastructure.Skillsoft.Olsa;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public class SkillsoftSsoService : ISkillsoftSsoService
{
    private const string TicketCacheKeyPrefix = "skillsoft-launch-ticket:";

    private readonly SkillsoftAccessGuard _accessGuard;
    private readonly ActiveLibraryCardResolver _cardResolver;
    private readonly IMemoryCache _cache;
    private readonly SkillsoftSsoSettings _settings;
    private readonly OlsaSoapClient _olsaClient;
    private readonly SkillsoftOlsaSettings _olsaSettings;

    public SkillsoftSsoService(
        SkillsoftAccessGuard accessGuard,
        ActiveLibraryCardResolver cardResolver,
        IMemoryCache cache,
        IOptions<SkillsoftSsoSettings> settings,
        OlsaSoapClient olsaClient,
        IOptions<SkillsoftOlsaSettings> olsaSettings)
    {
        _accessGuard = accessGuard;
        _cardResolver = cardResolver;
        _cache = cache;
        _settings = settings.Value;
        _olsaClient = olsaClient;
        _olsaSettings = olsaSettings.Value;
    }

    public async Task<string> CreateLaunchTicketAsync(CallerContext caller, int companyId, CancellationToken cancellationToken = default)
    {
        await _accessGuard.ResolveForCallerAsync(caller, companyId, cancellationToken);

        var ticket = Guid.NewGuid().ToString("N");
        _cache.Set(
            TicketCacheKeyPrefix + ticket,
            new LaunchTicketPayload(caller.DbUserId!.Value, companyId),
            TimeSpan.FromSeconds(Math.Max(5, _settings.LaunchTicketExpirySeconds)));

        return ticket;
    }

    public async Task<SkillsoftLaunchResult> ConsumeLaunchTicketAsync(string ticket, CancellationToken cancellationToken = default)
    {
        var cacheKey = TicketCacheKeyPrefix + ticket;
        if (!_cache.TryGetValue(cacheKey, out LaunchTicketPayload? payload) || payload is null)
        {
            throw new UnauthorizedAccessException("This launch link has expired or was already used. Go back and click \"Access the course library\" again.");
        }

        _cache.Remove(cacheKey);

        var card = await _cardResolver.ResolveAsync(payload.UserId, payload.CompanyId, cancellationToken);

        return BuildBypassLoginRedirect(card);
    }

    public async Task<string> GetCourseLaunchUrlAsync(CallerContext caller, int companyId, string assetId, CancellationToken cancellationToken = default)
    {
        var card = await _accessGuard.ResolveForCallerAsync(caller, companyId, cancellationToken);

        return await _olsaClient.GetMultiActionSignOnUrlAsync(
            _olsaSettings.CustomerId,
            actionType: "launch",
            assetId: assetId,
            userName: card.UserId,
            password: card.Password,
            firstName: card.FirstName,
            lastName: card.LastName,
            cancellationToken);
    }

    private SkillsoftLaunchResult BuildBypassLoginRedirect(Domain.Skillsoft.ActiveLibraryCard card)
    {
        if (string.IsNullOrWhiteSpace(_settings.BypassLoginBaseUrl))
        {
            throw new InvalidOperationException("Skillsoft BypassLogin is not configured. Set SkillsoftSso:BypassLoginBaseUrl.");
        }

        var url = $"{_settings.BypassLoginBaseUrl}" +
            $"?userName={Uri.EscapeDataString(card.UserId)}" +
            $"&password={Uri.EscapeDataString(card.Password)}" +
            $"&restype={Uri.EscapeDataString(_settings.BypassLoginRestype)}";

        return new SkillsoftLaunchResult(url);
    }

    private record LaunchTicketPayload(int UserId, int CompanyId);
}
