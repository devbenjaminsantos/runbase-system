using RunBase.Domain.Users;

namespace RunBase.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenService _accessTokenService;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _accessTokenService = accessTokenService;
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

        return AuthResult<AuthTokenResponse>.Success(new AuthTokenResponse(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            ToProfile(user)));
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
}
