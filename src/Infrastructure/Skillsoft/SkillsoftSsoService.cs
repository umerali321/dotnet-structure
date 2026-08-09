using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens.Saml2;
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
    private readonly IHostEnvironment _hostEnvironment;

    public SkillsoftSsoService(
        ApplicationDbContext dbContext,
        IUserDirectory userDirectory,
        IMemoryCache cache,
        IOptions<SkillsoftSsoSettings> settings,
        IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _userDirectory = userDirectory;
        _cache = cache;
        _settings = settings.Value;
        _hostEnvironment = hostEnvironment;
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

        var (card, user) = await ResolveEntitlementAsync(payload.UserId, payload.CompanyId, cancellationToken);

        return BuildSignedResponse(card.UserId, user.Email ?? card.Email ?? string.Empty, card.FirstName, card.LastName);
    }


    private async Task<(Domain.Skillsoft.ActiveLibraryCard Card, DirectoryUser User)> ResolveEntitlementAsync(
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

        var directoryUser = await _userDirectory.FindByIdentifierAsync(user.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Account not found.");

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

        return (card, directoryUser);
    }

    private SkillsoftLaunchResult BuildSignedResponse(string skillsoftUserId, string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(_settings.SkillsoftAcsUrl) || string.IsNullOrWhiteSpace(_settings.SkillsoftSpEntityId) || string.IsNullOrWhiteSpace(_settings.IdpEntityId))
        {
            throw new InvalidOperationException(
                "Skillsoft SSO is not configured yet. Set SkillsoftSso:IdpEntityId, SkillsoftSso:SkillsoftAcsUrl and " +
                "SkillsoftSso:SkillsoftSpEntityId (from Skillsoft's account team) before this can be used. " +
                "See docs/skillsoft-sso-checklist.md.");
        }

        var certificate = LoadSigningCertificate();

        var config = new Saml2Configuration
        {
            Issuer = _settings.IdpEntityId,
            SigningCertificate = certificate,
        };
        config.AllowedAudienceUris.Add(_settings.SkillsoftSpEntityId);

        var claims = new List<Claim>
        {
            new(_settings.FirstNameAttributeName, firstName),
            new(_settings.LastNameAttributeName, lastName),
            new(_settings.EmailAttributeName, email),
        };

        var response = new Saml2AuthnResponse(config)
        {
            Status = Saml2StatusCodes.Success,
            Destination = new Uri(_settings.SkillsoftAcsUrl),
            NameId = new Saml2NameIdentifier(skillsoftUserId, new Uri(_settings.NameIdFormat)),
            ClaimsIdentity = new ClaimsIdentity(claims),
        };
        response.CreateSecurityToken(
            _settings.SkillsoftSpEntityId,
            subjectConfirmationLifetime: 5,
            issuedTokenLifetime: Math.Max(1, _settings.AssertionValiditySeconds / 60));

        var binding = new Saml2PostBinding();
        binding.Bind(response);

        return new SkillsoftLaunchResult(binding.PostContent);
    }

    private X509Certificate2 LoadSigningCertificate()
    {
        if (!string.IsNullOrWhiteSpace(_settings.SigningCertificateBase64))
        {
            return X509CertificateLoader.LoadPkcs12(
                Convert.FromBase64String(_settings.SigningCertificateBase64),
                _settings.SigningCertificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);
        }

        if (!string.IsNullOrWhiteSpace(_settings.SigningCertificatePath))
        {
            return X509CertificateLoader.LoadPkcs12FromFile(
                _settings.SigningCertificatePath,
                _settings.SigningCertificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);
        }

        if (_settings.AllowDevSelfSignedCertificate && _hostEnvironment.IsDevelopment())
        {
            return DevSelfSignedCertificate.GetOrCreate();
        }

        throw new InvalidOperationException(
            "No Skillsoft SSO signing certificate is configured. Set SkillsoftSso:SigningCertificateBase64 " +
            "(+ SigningCertificatePassword) or SkillsoftSso:SigningCertificatePath. See docs/skillsoft-sso-checklist.md.");
    }

    private record LaunchTicketPayload(int UserId, int CompanyId);
}
