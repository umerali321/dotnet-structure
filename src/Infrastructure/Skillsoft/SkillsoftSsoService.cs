using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Infrastructure.Options;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public class SkillsoftSsoService : ISkillsoftSsoService
{
    private const string TicketCacheKeyPrefix = "skillsoft-launch-ticket:";

    private readonly ApplicationDbContext _dbContext;
    private readonly IUserDirectory _userDirectory;
    private readonly IMemoryCache _cache;
    private readonly SkillsoftSsoSettings _settings;

    public SkillsoftSsoService(
        ApplicationDbContext dbContext,
        IUserDirectory userDirectory,
        IMemoryCache cache,
        IOptions<SkillsoftSsoSettings> settings)
    {
        _dbContext = dbContext;
        _userDirectory = userDirectory;
        _cache = cache;
        _settings = settings.Value;
    }

    public async Task<string> CreateLaunchTicketAsync(CallerContext caller, int companyId, CancellationToken cancellationToken = default)
    {
        var userId = caller.DbUserId ?? throw new UnauthorizedAccessException("Not authenticated.");

        var activeCompanyRoles = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);
        if (!activeCompanyRoles.Any(r => r.CompanyId == companyId))
        {
            throw new UnauthorizedAccessException("You do not have an active role at that company.");
        }

        await ResolveEntitlementAsync(userId, companyId, cancellationToken);

        var ticket = Guid.NewGuid().ToString("N");
        _cache.Set(
            TicketCacheKeyPrefix + ticket,
            new LaunchTicketPayload(userId, companyId),
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

        var card = await ResolveEntitlementAsync(payload.UserId, payload.CompanyId, cancellationToken);

        return BuildBypassLoginRedirect(card);
    }

    private async Task<Domain.Skillsoft.ActiveLibraryCard> ResolveEntitlementAsync(
        int userId, int companyId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (user?.Email is null)
        {
            throw new UnauthorizedAccessException("Your account has no email on file - Skillsoft access requires one.");
        }

        var companyCode = await _dbContext.Companies.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .Select(c => c.CompanyCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(companyCode))
        {
            throw new UnauthorizedAccessException("Company not found.");
        }

        var today = DateTime.UtcNow.Date;

        var card = await _dbContext.ActiveLibraryCards.AsNoTracking()
            .Where(c => c.CompanyCode == companyCode
                && c.Email != null && c.Email.ToLower() == user.Email.ToLower()
                && c.StartDate <= today
                && c.EndDate >= today)
            .OrderByDescending(c => c.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
        {
            throw new UnauthorizedAccessException("You do not have an active Skillsoft library card for this company.");
        }

        return card;
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
