using RunBase.Domain.Users;

namespace RunBase.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokens;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _refreshTokens = refreshTokens;
    }

    public async Task<AuthResult<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return AuthResult<AuthTokenResponse>.Failure(AuthError.InvalidCredentials);
        }

        if (!user.CanAuthenticate)
        {
            return AuthResult<AuthTokenResponse>.Failure(AuthError.InactiveUser);
        }

        var accessToken = _accessTokenService.Create(user);
        var refreshToken = _refreshTokenService.Create(user);
        await _refreshTokens.SaveAsync(refreshToken, cancellationToken);

        return AuthResult<AuthTokenResponse>.Success(ToTokenResponse(
            user,
            accessToken,
            refreshToken));
    }

    public async Task<AuthResult<AuthTokenResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var storedRefreshToken = await _refreshTokens.GetByValueAsync(
            request.RefreshToken,
            cancellationToken);

        if (storedRefreshToken is null || !storedRefreshToken.IsActive(DateTimeOffset.UtcNow))
        {
            return AuthResult<AuthTokenResponse>.Failure(AuthError.InvalidRefreshToken);
        }

        var user = await _users.GetByIdAsync(storedRefreshToken.UserId, cancellationToken);

        if (user is null)
        {
            return AuthResult<AuthTokenResponse>.Failure(AuthError.UserNotFound);
        }

        if (!user.CanAuthenticate)
        {
            return AuthResult<AuthTokenResponse>.Failure(AuthError.InactiveUser);
        }

        storedRefreshToken.Revoke(DateTimeOffset.UtcNow);

        var accessToken = _accessTokenService.Create(user);
        var nextRefreshToken = _refreshTokenService.Create(user);
        await _refreshTokens.SaveAsync(nextRefreshToken, cancellationToken);

        return AuthResult<AuthTokenResponse>.Success(ToTokenResponse(
            user,
            accessToken,
            nextRefreshToken));
    }

    public async Task<AuthResult<UserProfileResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return AuthResult<UserProfileResponse>.Failure(AuthError.UserNotFound);
        }

        return AuthResult<UserProfileResponse>.Success(ToProfile(user));
    }

    private static UserProfileResponse ToProfile(User user)
    {
        return new UserProfileResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.Status);
    }

    private static AuthTokenResponse ToTokenResponse(
        User user,
        AccessToken accessToken,
        RefreshToken refreshToken)
    {
        return new AuthTokenResponse(
            accessToken.Value,
            refreshToken.Value,
            accessToken.ExpiresAtUtc,
            ToProfile(user));
    }
}
