using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunBase.Application.Auth;
using RunBase.Application.Clients;
using RunBase.Application.Orders;
using RunBase.Application.Plans;
using RunBase.Application.Security;
using RunBase.Infrastructure.Auth;
using RunBase.Infrastructure.Clients;
using RunBase.Infrastructure.Orders;
using RunBase.Infrastructure.Plans;
using RunBase.Infrastructure.Security;

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
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IRefreshTokenService, RandomRefreshTokenService>();
        services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
        services.AddSingleton<IClientRepository, InMemoryClientRepository>();
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<IPlanRepository, InMemoryPlanRepository>();
        services.AddSingleton<ISensitiveDataAuditRepository, InMemorySensitiveDataAuditRepository>();

        return services;
    }
}
