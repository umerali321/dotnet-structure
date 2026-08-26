using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Domain.Skillsoft;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public record SkillportSessionStatus(bool HasActiveSession, bool IsExpired, bool HasDormantAccount, DateOnly? StartDate, DateOnly? EndDate);


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

    /// <summary>Whether the caller currently has an active session, without changing anything - also adopts a matching legacy ActiveLibraryCards entitlement into our table the first time it's seen.</summary>
    public async Task<SkillportSessionStatus> GetStatusAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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
            var legacyStart = DateOnly.FromDateTime(legacyCard.StartDate);
            var legacyEnd = DateOnly.FromDateTime(legacyCard.EndDate);

            _dbContext.SkillportSessions.Add(SkillportSession.CreateActive(
                userId, companyId, legacyCard.UserId, legacyCard.Password, legacyStart, legacyEnd));
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new SkillportSessionStatus(true, false, false, legacyStart, legacyEnd);
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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

        // A dormant account already exists over on Skillport under this identity (legacy path -
        // created ahead of time, never activated yet) - just start its 30-day clock. We can't
        // rename an account that already exists there, so this is the one case that keeps reusing
        // an existing username/password.
        if (session is not null && session.IsDormant)
        {
            var entitlement = new SkillsoftEntitlementRequest(
                companyId, session.SkillportUsername, session.SkillportPassword, user.FirstName, user.LastName, user.Email,
                managerEmail ?? user.Email, managerName ?? "Unassigned");

            var entitlementResult = await _provisioningService.RecordEntitlementAsync(entitlement, cancellationToken);
            if (!entitlementResult.Success)
            {
                return entitlementResult;
            }

            session.Activate(today, today.AddDays(30));
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new SkillsoftProvisionResult(true, null);
        }

        // No account at all, or the previous one expired - either way, a brand-new Skillport
        // identity: a fresh random username, and the caller's current app password unless an
        // explicit one was supplied (the admin-triggered "retry from profile" flow). The expired
        // row (if any) is left alone as history.
        var newUsername = await GenerateUniqueSkillportUsernameAsync(cancellationToken);
        var password = initialPassword ?? user.PasswordHash;

        var accountResult = await _provisioningService.CreateAccountAsync(newUsername, password, user.FirstName, user.LastName, cancellationToken);
        if (!accountResult.Success)
        {
            return accountResult;
        }

        var newEntitlementResult = await _provisioningService.RecordEntitlementAsync(
            new SkillsoftEntitlementRequest(
                companyId, newUsername, password, user.FirstName, user.LastName, user.Email,
                managerEmail ?? user.Email, managerName ?? "Unassigned"),
            cancellationToken);

        if (!newEntitlementResult.Success)
        {
            return newEntitlementResult;
        }

        _dbContext.SkillportSessions.Add(SkillportSession.CreateActive(userId, companyId, newUsername, password, today, today.AddDays(30)));
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

    /// <summary>Builds a random "10LC######" Skillport username (10 characters total) and retries
    /// on the rare chance of a collision against every username we know of - both our own
    /// SkillportSessions history and the legacy ActiveLibraryCards table.</summary>
    private async Task<string> GenerateUniqueSkillportUsernameAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 20;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var suffix = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
            var candidate = $"10LC{suffix}";

            var inUse = await _dbContext.SkillportSessions.AnyAsync(s => s.SkillportUsername == candidate, cancellationToken)
                || await _dbContext.ActiveLibraryCards.AnyAsync(c => c.UserId == candidate, cancellationToken);

            if (!inUse)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique Skillport username.");
    }
}
