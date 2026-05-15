using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RunBase.Application.Auth;
using RunBase.Domain.Users;

namespace RunBase.Infrastructure.Auth;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public InMemoryUserRepository(
        IOptions<AuthSeedOptions> seedOptions,
        IPasswordHasher passwordHasher)
    {
        var seed = seedOptions.Value;
        var now = DateTimeOffset.UtcNow;

        var admin = new User(
                Guid.Parse(seed.Id),
                seed.Name,
                seed.Email,
                passwordHasher.Hash(seed.Password),
                UserRole.Admin,
                UserStatus.Active,
                now,
                now);

        _users[admin.Id] = admin;
    }

    public Task<IReadOnlyList<User>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<User>>(_users.Values.ToList());
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(user =>
            string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
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

    public Task SaveAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        _users[user.Id] = user;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        _users.TryRemove(user.Id, out _);

        return Task.CompletedTask;
    }
}
