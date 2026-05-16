using Microsoft.EntityFrameworkCore;
using RunBase.Application.Auth;
using RunBase.Infrastructure.Persistence;

namespace RunBase.Infrastructure.Auth;

public sealed class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly RunBaseDbContext _dbContext;

    public EfRefreshTokenRepository(RunBaseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        var isTracked = _dbContext.ChangeTracker
            .Entries<RefreshToken>()
            .Any(entry => entry.Entity.Value == refreshToken.Value);

        if (!isTracked)
        {
            var exists = await _dbContext.RefreshTokens.AnyAsync(
                storedToken => storedToken.Value == refreshToken.Value,
                cancellationToken);

            if (exists)
            {
                _dbContext.RefreshTokens.Update(refreshToken);
            }
            else
            {
                await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByValueAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens.FirstOrDefaultAsync(
            refreshToken => refreshToken.Value == value,
            cancellationToken);
    }

    public async Task RevokeAsync(
        RefreshToken refreshToken,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        refreshToken.Revoke(revokedAtUtc);

        await SaveAsync(refreshToken, cancellationToken);
    }
}
