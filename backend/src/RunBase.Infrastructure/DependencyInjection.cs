using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunBase.Application.Auth;
using RunBase.Infrastructure.Auth;

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

        return services;
    }
}
