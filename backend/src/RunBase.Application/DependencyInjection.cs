using Microsoft.Extensions.DependencyInjection;
using RunBase.Application.Auth;
using RunBase.Application.Health;

namespace RunBase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHealthStatusService, HealthStatusService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
