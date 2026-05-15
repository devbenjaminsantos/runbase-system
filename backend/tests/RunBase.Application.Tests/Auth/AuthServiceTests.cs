using RunBase.Application.Auth;
using RunBase.Domain.Users;

namespace RunBase.Application.Tests.Auth;

public sealed class AuthServiceTests
{
    private static readonly Guid ActiveUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccessToken()
    {
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequest("admin@runbase.local", "Admin123!"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("test-access-token", result.Value.AccessToken);
        Assert.Equal("test-refresh-token-1", result.Value.RefreshToken);
        Assert.Equal(ActiveUserId, result.Value.User.Id);
        Assert.Equal(UserRole.Admin, result.Value.User.Role);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsInvalidCredentials()
    {
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequest("admin@runbase.local", "wrong-password"));

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsInactiveUser()
    {
        var inactiveUser = CreateUser(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "inactive@runbase.local",
            UserStatus.Inactive);
        var service = CreateService(extraUsers: inactiveUser);

        var result = await service.LoginAsync(new LoginRequest("inactive@runbase.local", "Admin123!"));

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.InactiveUser, result.Error);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithExistingUser_ReturnsProfile()
    {
        var service = CreateService();

        var result = await service.GetCurrentUserAsync(ActiveUserId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("RunBase Admin", result.Value.Name);
        Assert.Equal("admin@runbase.local", result.Value.Email);
    }

    [Fact]
    public async Task RefreshAsync_WithValidRefreshToken_ReturnsNewTokenPair()
    {
        var refreshTokens = new FakeRefreshTokenRepository();
        var service = CreateService(refreshTokens: refreshTokens);
        var login = await service.LoginAsync(new LoginRequest("admin@runbase.local", "Admin123!"));

        var result = await service.RefreshAsync(new RefreshTokenRequest(login.Value!.RefreshToken));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("test-access-token", result.Value.AccessToken);
        Assert.Equal("test-refresh-token-2", result.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_WhenRefreshTokenIsReused_ReturnsInvalidRefreshToken()
    {
        var refreshTokens = new FakeRefreshTokenRepository();
        var service = CreateService(refreshTokens: refreshTokens);
        var login = await service.LoginAsync(new LoginRequest("admin@runbase.local", "Admin123!"));

        await service.RefreshAsync(new RefreshTokenRequest(login.Value!.RefreshToken));
        var result = await service.RefreshAsync(new RefreshTokenRequest(login.Value.RefreshToken));

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task LogoutAsync_WithValidRefreshToken_RevokesRefreshToken()
    {
        var refreshTokens = new FakeRefreshTokenRepository();
        var service = CreateService(refreshTokens: refreshTokens);
        var login = await service.LoginAsync(new LoginRequest("admin@runbase.local", "Admin123!"));

        var logout = await service.LogoutAsync(new LogoutRequest(login.Value!.RefreshToken));
        var refresh = await service.RefreshAsync(new RefreshTokenRequest(login.Value.RefreshToken));

        Assert.True(logout.Succeeded);
        Assert.False(refresh.Succeeded);
        Assert.Equal(AuthError.InvalidRefreshToken, refresh.Error);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidRefreshToken_ReturnsInvalidRefreshToken()
    {
        var service = CreateService();

        var result = await service.LogoutAsync(new LogoutRequest("missing-refresh-token"));

        Assert.False(result.Succeeded);
        Assert.Equal(AuthError.InvalidRefreshToken, result.Error);
    }

    private static AuthService CreateService(
        FakeRefreshTokenRepository? refreshTokens = null,
        params User[] extraUsers)
    {
        var users = new List<User>
        {
            CreateUser(ActiveUserId, "admin@runbase.local", UserStatus.Active)
        };
        users.AddRange(extraUsers);

        return new AuthService(
            new FakeUserRepository(users),
            new FakePasswordHasher(),
            new FakeAccessTokenService(),
            new FakeRefreshTokenService(),
            refreshTokens ?? new FakeRefreshTokenRepository());
    }

    private static User CreateUser(Guid id, string email, UserStatus status)
    {
        var now = DateTimeOffset.UtcNow;

        return new User(
            id,
            email == "admin@runbase.local" ? "RunBase Admin" : "Inactive User",
            email,
            "Admin123!",
            UserRole.Admin,
            status,
            now,
            now);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly IReadOnlyList<User> _users;

        public FakeUserRepository(IReadOnlyList<User> users)
        {
            _users = users;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(user => user.Id == id));
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return password;
        }

        public bool Verify(string password, string passwordHash)
        {
            return password == passwordHash;
        }
    }

    private sealed class FakeAccessTokenService : IAccessTokenService
    {
        public AccessToken Create(User user)
        {
            return new AccessToken("test-access-token", DateTimeOffset.UtcNow.AddMinutes(30));
        }
    }

    private sealed class FakeRefreshTokenService : IRefreshTokenService
    {
        private int _sequence;

        public RefreshToken Create(User user)
        {
            _sequence += 1;

            return new RefreshToken(
                $"test-refresh-token-{_sequence}",
                user.Id,
                DateTimeOffset.UtcNow.AddDays(7),
                DateTimeOffset.UtcNow);
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly Dictionary<string, RefreshToken> _refreshTokens = new();

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
}
