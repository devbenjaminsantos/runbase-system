using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using RunBase.Application.Auth;
using RunBase.Domain.Users;

namespace RunBase.Infrastructure.Auth;

public sealed class RandomRefreshTokenService : IRefreshTokenService
{
    private readonly JwtOptions _options;

    public RandomRefreshTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public RefreshToken Create(User user)
    {
        var now = DateTimeOffset.UtcNow;
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new RefreshToken(
            value,
            user.Id,
            now.AddDays(_options.RefreshTokenDays),
            now);
    }
}
