using Microsoft.Extensions.DependencyInjection;
using RunBase.Application.Health;

namespace RunBase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHealthStatusService, HealthStatusService>();

        return services;
    }
}
