namespace RunBase.Application.Auth;

public interface IAuthService
{
    Task<AuthResult<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<AuthTokenResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<bool>> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<UserProfileResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
