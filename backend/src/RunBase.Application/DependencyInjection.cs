using Microsoft.Extensions.DependencyInjection;
using RunBase.Application.Auth;
using RunBase.Application.Clients;
using RunBase.Application.Demo;
using RunBase.Application.Health;
using RunBase.Application.Orders;
using RunBase.Application.Plans;
using RunBase.Application.Security;
using RunBase.Application.Users;

namespace RunBase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHealthStatusService, HealthStatusService>();
        services.AddSingleton<ISensitiveLogSanitizer, SensitiveLogSanitizer>();
        services.AddSingleton<ISensitiveDataMasker, SensitiveDataMasker>();
        services.AddSingleton<IDemoDataGenerator, DemoDataGenerator>();
        services.AddScoped<ISensitiveDataAccessAuditor, SensitiveDataAccessAuditor>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClientsService, ClientsService>();
        services.AddScoped<IOrdersService, OrdersService>();
        services.AddScoped<IPlansService, PlansService>();
        services.AddScoped<IUsersService, UsersService>();

        return services;
    }
}
