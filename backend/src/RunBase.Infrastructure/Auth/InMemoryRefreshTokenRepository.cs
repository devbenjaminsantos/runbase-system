using System.Collections.Concurrent;
using RunBase.Application.Auth;

namespace RunBase.Infrastructure.Auth;

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ConcurrentDictionary<string, RefreshToken> _refreshTokens = new();

    public Task SaveAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        _refreshTokens[refreshToken.Value] = refreshToken;

        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByValueAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        _refreshTokens.TryGetValue(value, out var refreshToken);

        return Task.FromResult(refreshToken);
    }

    public Task RevokeAsync(
        RefreshToken refreshToken,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        refreshToken.Revoke(revokedAtUtc);
        _refreshTokens[refreshToken.Value] = refreshToken;

        return Task.CompletedTask;
    }
}
