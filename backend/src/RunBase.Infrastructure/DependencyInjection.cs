using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RunBase.Application.Auth;
using RunBase.Application.Clients;
using RunBase.Application.Notifications;
using RunBase.Application.Orders;
using RunBase.Application.Plans;
using RunBase.Application.Security;
using RunBase.Infrastructure.Auth;
using RunBase.Infrastructure.Clients;
using RunBase.Infrastructure.Notifications;
using RunBase.Infrastructure.Orders;
using RunBase.Infrastructure.Persistence;
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
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<RunBaseDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IClientRepository, EfClientRepository>();
            services.AddScoped<INotificationCampaignRepository, EfNotificationCampaignRepository>();
            services.AddScoped<IOrderRepository, EfOrderRepository>();
            services.AddScoped<IPlanRepository, EfPlanRepository>();
            services.AddScoped<ISensitiveDataAuditRepository, EfSensitiveDataAuditRepository>();
        }
        else
        {
            services.AddSingleton<IClientRepository, InMemoryClientRepository>();
            services.AddSingleton<INotificationCampaignRepository, InMemoryNotificationCampaignRepository>();
            services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
            services.AddSingleton<IPlanRepository, InMemoryPlanRepository>();
            services.AddSingleton<ISensitiveDataAuditRepository, InMemorySensitiveDataAuditRepository>();
        }

        services.Configure<AuthSeedOptions>(configuration.GetSection(AuthSeedOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SensitiveDataProtectionOptions>(
            configuration.GetSection(SensitiveDataProtectionOptions.SectionName));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISensitiveDataProtector, AesGcmSensitiveDataProtector>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IRefreshTokenService, RandomRefreshTokenService>();
        services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();

        return services;
    }
}
