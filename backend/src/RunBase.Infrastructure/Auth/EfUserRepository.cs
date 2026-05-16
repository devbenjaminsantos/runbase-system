using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RunBase.Application.Auth;
using RunBase.Domain.Users;
using RunBase.Infrastructure.Persistence;

namespace RunBase.Infrastructure.Auth;

public sealed class EfUserRepository : IUserRepository
{
    private readonly RunBaseDbContext _dbContext;
    private readonly AuthSeedOptions _seedOptions;
    private readonly IPasswordHasher _passwordHasher;

    public EfUserRepository(
        RunBaseDbContext dbContext,
        IOptions<AuthSeedOptions> seedOptions,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _seedOptions = seedOptions.Value;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureSeedAdminAsync(cancellationToken);

        return await _dbContext.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeedAdminAsync(cancellationToken);

        var normalizedEmail = NormalizeEmail(email);

        return await _dbContext.Users.FirstOrDefaultAsync(
            user => user.Email.ToUpper() == normalizedEmail,
            cancellationToken);
    }

    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeedAdminAsync(cancellationToken);

        return await _dbContext.Users.FirstOrDefaultAsync(
            user => user.Id == id,
            cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? exceptUserId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeedAdminAsync(cancellationToken);

        var normalizedEmail = NormalizeEmail(email);

        return await _dbContext.Users.AnyAsync(
            user =>
                (!exceptUserId.HasValue || user.Id != exceptUserId.Value) &&
                user.Email.ToUpper() == normalizedEmail,
            cancellationToken);
    }

    public async Task SaveAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var isTracked = _dbContext.ChangeTracker
            .Entries<User>()
            .Any(entry => entry.Entity.Id == user.Id);

        if (!isTracked)
        {
            var exists = await _dbContext.Users.AnyAsync(
                existingUser => existingUser.Id == user.Id,
                cancellationToken);

            if (exists)
            {
                _dbContext.Users.Update(user);
            }
            else
            {
                await _dbContext.Users.AddAsync(user, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Remove(user);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSeedAdminAsync(CancellationToken cancellationToken)
    {
        var seedId = Guid.Parse(_seedOptions.Id);
        var seedEmail = NormalizeEmail(_seedOptions.Email);
        var seedExists = await _dbContext.Users.AnyAsync(
            user => user.Id == seedId || user.Email.ToUpper() == seedEmail,
            cancellationToken);

        if (seedExists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var admin = new User(
            seedId,
            _seedOptions.Name,
            _seedOptions.Email,
            _passwordHasher.Hash(_seedOptions.Password),
            UserRole.Admin,
            UserStatus.Active,
            now,
            now);

        await _dbContext.Users.AddAsync(admin, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
