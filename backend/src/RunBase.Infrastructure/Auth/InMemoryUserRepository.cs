using Microsoft.Extensions.Options;
using RunBase.Application.Auth;
using RunBase.Domain.Users;

namespace RunBase.Infrastructure.Auth;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly IReadOnlyList<User> _users;

    public InMemoryUserRepository(
        IOptions<AuthSeedOptions> seedOptions,
        IPasswordHasher passwordHasher)
    {
        var seed = seedOptions.Value;
        var now = DateTimeOffset.UtcNow;

        _users =
        [
            new User(
                Guid.Parse(seed.Id),
                seed.Name,
                seed.Email,
                passwordHasher.Hash(seed.Password),
                UserRole.Admin,
                UserStatus.Active,
                now,
                now)
        ];
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(user =>
            string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(user => user.Id == id);

        return Task.FromResult(user);
    }
}
