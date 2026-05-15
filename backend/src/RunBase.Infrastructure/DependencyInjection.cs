using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunBase.Application.Auth;
using RunBase.Application.Clients;
using RunBase.Infrastructure.Auth;
using RunBase.Infrastructure.Clients;

namespace RunBase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;
        services.Configure<AuthSeedOptions>(configuration.GetSection(AuthSeedOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PlainTextPasswordHasher>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IRefreshTokenService, RandomRefreshTokenService>();
        services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
        services.AddSingleton<IClientRepository, InMemoryClientRepository>();

        return services;
    }
}
