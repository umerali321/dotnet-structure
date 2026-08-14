using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Domain.Skillsoft;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public record SkillportSessionStatus(bool HasActiveSession, bool IsExpired, bool HasDormantAccount, DateTime? StartDate, DateTime? EndDate);


public class SkillportSessionManager : ISkillportSessionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ActiveLibraryCardResolver _cardResolver;
    private readonly StudentManagerResolver _managerResolver;
    private readonly ISkillsoftProvisioningService _provisioningService;

    public SkillportSessionManager(
        ApplicationDbContext dbContext,
        ActiveLibraryCardResolver cardResolver,
        StudentManagerResolver managerResolver,
        ISkillsoftProvisioningService provisioningService)
    {
        _dbContext = dbContext;
        _cardResolver = cardResolver;
        _managerResolver = managerResolver;
        _provisioningService = provisioningService;
    }

    /// <summary>Called after a user is registered - creates their Skillport account right away, but dormant (no session window yet). Best-effort: a failure here does not throw, it just means EnsureActiveAsync will try again from scratch later.</summary>
    public async Task<SkillsoftProvisionResult> EnsureDormantAccountAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        var existing = await LatestSessionAsync(userId, companyId, cancellationToken);
        if (existing is not null)
        {
            return new SkillsoftProvisionResult(true, null);
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return new SkillsoftProvisionResult(false, "User not found.");
        }

        // New Skillport registration - a random numeric password, not the user's own app password
        // (matches the legacy convention: real ActiveLibraryCards rows use short numeric PINs).
        var skillportPassword = GenerateRandomPassword();

        var accountResult = await _provisioningService.CreateAccountAsync(user.Username, skillportPassword, user.FirstName, user.LastName, cancellationToken);
        if (!accountResult.Success)
        {
            return accountResult;
        }

        _dbContext.SkillportSessions.Add(SkillportSession.CreateDormant(userId, companyId, user.Username, skillportPassword));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SkillsoftProvisionResult(true, null);
    }

    /// <summary>Whether the caller currently has an active session, without changing anything - also adopts a matching legacy ActiveLibraryCards entitlement into our table the first time it's seen.</summary>
    public async Task<SkillportSessionStatus> GetStatusAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var session = await LatestSessionAsync(userId, companyId, cancellationToken);

        if (session is not null && session.IsActive(today))
        {
            return new SkillportSessionStatus(true, false, false, session.StartDate, session.EndDate);
        }

        if (session is not null && session.IsExpired(today))
        {
            return new SkillportSessionStatus(false, true, false, session.StartDate, session.EndDate);
        }

        if (session is not null && session.IsDormant)
        {
            return new SkillportSessionStatus(false, false, true, null, null);
        }

        var legacyCard = await _cardResolver.TryResolveAsync(userId, companyId, cancellationToken);
        if (legacyCard is not null)
        {
            _dbContext.SkillportSessions.Add(SkillportSession.CreateActive(
                userId, companyId, legacyCard.UserId, legacyCard.Password, legacyCard.StartDate, legacyCard.EndDate));
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new SkillportSessionStatus(true, false, false, legacyCard.StartDate, legacyCard.EndDate);
        }

        return new SkillportSessionStatus(false, false, false, null, null);
    }

    
    public async Task<SkillsoftProvisionResult> EnsureActiveAsync(int userId, int companyId, CancellationToken cancellationToken = default)
        => await EnsureActiveInternalAsync(userId, companyId, initialPassword: null, cancellationToken);

    public async Task<SkillsoftProvisionResult> CreateNewSessionAsync(int userId, int companyId, string password, CancellationToken cancellationToken = default)
        => await EnsureActiveInternalAsync(userId, companyId, initialPassword: password, cancellationToken);

    private async Task<SkillsoftProvisionResult> EnsureActiveInternalAsync(
        int userId, int companyId, string? initialPassword, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var session = await LatestSessionAsync(userId, companyId, cancellationToken);

        if (session is not null && session.IsActive(today))
        {
            return new SkillsoftProvisionResult(true, null);
        }

        var user = await GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return new SkillsoftProvisionResult(false, "User not found.");
        }

        var (managerEmail, managerName) = await _managerResolver.ResolveAsync(userId, companyId, cancellationToken);

        if (session is not null)
        {
            var entitlement = new SkillsoftEntitlementRequest(
                companyId, session.SkillportUsername, session.SkillportPassword, user.FirstName, user.LastName, user.Email,
                managerEmail ?? user.Email, managerName ?? "Unassigned");

            var entitlementResult = await _provisioningService.RecordEntitlementAsync(entitlement, cancellationToken);
            if (!entitlementResult.Success)
            {
                return entitlementResult;
            }

            if (session.IsDormant)
            {
                session.Activate(today, today.AddDays(30));
            }
            else
            {
                // Expired - leave that row as history, add a new one for this restart, same identity.
                _dbContext.SkillportSessions.Add(SkillportSession.CreateActive(
                    userId, companyId, session.SkillportUsername, session.SkillportPassword, today, today.AddDays(30)));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new SkillsoftProvisionResult(true, null);
        }

        // No account at all - the only case that ever registers a brand-new Skillport identity.
        var password = initialPassword ?? GenerateRandomPassword();

        var accountResult = await _provisioningService.CreateAccountAsync(user.Username, password, user.FirstName, user.LastName, cancellationToken);
        if (!accountResult.Success)
        {
            return accountResult;
        }

        var newEntitlementResult = await _provisioningService.RecordEntitlementAsync(
            new SkillsoftEntitlementRequest(
                companyId, user.Username, password, user.FirstName, user.LastName, user.Email,
                managerEmail ?? user.Email, managerName ?? "Unassigned"),
            cancellationToken);

        if (!newEntitlementResult.Success)
        {
            return newEntitlementResult;
        }

        _dbContext.SkillportSessions.Add(SkillportSession.CreateActive(userId, companyId, user.Username, password, today, today.AddDays(30)));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SkillsoftProvisionResult(true, null);
    }

    private Task<SkillportSession?> LatestSessionAsync(int userId, int companyId, CancellationToken cancellationToken) =>
        _dbContext.SkillportSessions
            .Where(s => s.UserId == userId && s.CompanyId == companyId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<UserSnapshot?> GetUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Username, u.PasswordHash, u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(user?.Username) || string.IsNullOrWhiteSpace(user.PasswordHash) || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        return new UserSnapshot(SanitizeUsername(user.Username), user.PasswordHash, user.FirstName ?? string.Empty, user.LastName ?? string.Empty, user.Email);
    }

    private static string SanitizeUsername(string username)
    {
        var atIndex = username.IndexOf('@');
        return atIndex > 0 ? username[..atIndex] : username;
    }

    private record UserSnapshot(string Username, string PasswordHash, string FirstName, string LastName, string Email);

    private static string GenerateRandomPassword()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 100_000_000;
        return value.ToString("D8");
    }
}
