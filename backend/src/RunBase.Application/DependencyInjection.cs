using Microsoft.Extensions.DependencyInjection;
using RunBase.Application.Auth;
using RunBase.Application.Health;
using RunBase.Application.Users;

namespace RunBase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHealthStatusService, HealthStatusService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsersService, UsersService>();

        return services;
    }
}
