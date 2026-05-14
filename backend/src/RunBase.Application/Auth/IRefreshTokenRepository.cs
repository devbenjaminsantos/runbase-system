namespace RunBase.Application.Auth;

public interface IRefreshTokenRepository
{
    Task SaveAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByValueAsync(
        string value,
        CancellationToken cancellationToken = default);
}
