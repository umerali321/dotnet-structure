using System.Collections.Concurrent;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Auth;

/// <summary>
/// Temporary in-memory store used until a real database is connected. Tokens are lost on
/// restart and not shared across instances. Swap the DI registration in
/// <see cref="DependencyInjection"/> for an EF-Core-backed <see cref="IRefreshTokenRepository"/>
/// once the database is available; <see cref="IRefreshTokenRepository"/> and every caller of it
/// stay unchanged.
/// </summary>
public class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _tokens[refreshToken.Token] = refreshToken;
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        _tokens.TryGetValue(token, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _tokens[refreshToken.Token] = refreshToken;
        return Task.CompletedTask;
    }
}
