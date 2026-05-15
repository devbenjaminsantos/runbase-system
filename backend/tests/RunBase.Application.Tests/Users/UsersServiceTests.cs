using RunBase.Application.Auth;
using RunBase.Application.Users;
using RunBase.Domain.Users;

namespace RunBase.Application.Tests.Users;

public sealed class UsersServiceTests
{
    [Fact]
    public async Task CreateAsync_WithExplicitRole_CreatesUser()
    {
        var service = CreateService();

        var result = await service.CreateAsync(new CreateUserRequest(
            "Support User",
            "support@runbase.local",
            "Support123!",
            UserRole.Support,
            UserStatus.Active));

        Assert.True(result.Succeeded);
        Assert.Equal(UserRole.Support, result.Value!.Role);
        Assert.Equal(UserStatus.Active, result.Value.Status);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatedEmail_ReturnsEmailAlreadyExists()
    {
        var service = CreateService(CreateUser("admin@runbase.local"));

        var result = await service.CreateAsync(new CreateUserRequest(
            "Another Admin",
            "ADMIN@runbase.local",
            "Admin123!",
            UserRole.Admin,
            UserStatus.Active));

        Assert.False(result.Succeeded);
        Assert.Equal(UserError.EmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesRoleAndStatus()
    {
        var user = CreateUser("manager@runbase.local", UserRole.Manager);
        var service = CreateService(user);

        var result = await service.UpdateAsync(user.Id, new UpdateUserRequest(
            "Viewer User",
            "viewer@runbase.local",
            UserRole.Viewer,
            UserStatus.Inactive));

        Assert.True(result.Succeeded);
        Assert.Equal(UserRole.Viewer, result.Value!.Role);
        Assert.Equal(UserStatus.Inactive, result.Value.Status);
        Assert.Equal("viewer@runbase.local", result.Value.Email);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        var user = CreateUser("manager@runbase.local", UserRole.Manager);
        var service = CreateService(user);

        var delete = await service.DeleteAsync(user.Id);
        var get = await service.GetByIdAsync(user.Id);

        Assert.True(delete.Succeeded);
        Assert.False(get.Succeeded);
        Assert.Equal(UserError.NotFound, get.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIsLastActiveAdmin_ReturnsLastActiveAdminRequired()
    {
        var admin = CreateUser("admin@runbase.local");
        var service = CreateService(admin);

        var result = await service.UpdateAsync(admin.Id, new UpdateUserRequest(
            "Viewer User",
            "viewer@runbase.local",
            UserRole.Viewer,
            UserStatus.Active));

        Assert.False(result.Succeeded);
        Assert.Equal(UserError.LastActiveAdminRequired, result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsLastActiveAdmin_ReturnsLastActiveAdminRequired()
    {
        var admin = CreateUser("admin@runbase.local");
        var service = CreateService(admin);

        var result = await service.DeleteAsync(admin.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(UserError.LastActiveAdminRequired, result.Error);
    }

    private static UsersService CreateService(params User[] users)
    {
        return new UsersService(
            new FakeUserRepository(users),
            new FakePasswordHasher());
    }

    private static User CreateUser(
        string email,
        UserRole role = UserRole.Admin)
    {
        var now = DateTimeOffset.UtcNow;

        return new User(
            Guid.NewGuid(),
            "Test User",
            email,
            "password-hash",
            role,
            UserStatus.Active,
            now,
            now);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users;

        public FakeUserRepository(IReadOnlyList<User> users)
        {
            _users = users.ToDictionary(user => user.Id);
        }

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(_users.Values.ToList());
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.Values.FirstOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.TryGetValue(id, out var user);

            return Task.FromResult(user);
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? exceptUserId = null,
            CancellationToken cancellationToken = default)
        {
            var exists = _users.Values.Any(user =>
                user.Id != exceptUserId &&
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task SaveAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Id] = user;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Remove(user.Id);

            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"hashed:{password}";
        }

        public bool Verify(string password, string passwordHash)
        {
            return passwordHash == $"hashed:{password}";
        }
    }
}
