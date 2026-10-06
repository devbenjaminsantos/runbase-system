using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

namespace RunBase.Application.Tests.Api;

internal static class InMemoryApiFactory
{
    public static WebApplicationFactory<Program> Create()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = string.Empty
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<RunBaseDbContext>();
                    services.RemoveAll<DbContextOptions<RunBaseDbContext>>();
                    services.RemoveAll<IClientRepository>();
                    services.RemoveAll<INotificationCampaignRepository>();
                    services.RemoveAll<IOrderRepository>();
                    services.RemoveAll<IPlanRepository>();
                    services.RemoveAll<ISensitiveDataAuditRepository>();
                    services.RemoveAll<IUserRepository>();
                    services.RemoveAll<IRefreshTokenRepository>();

                    services.AddSingleton<IClientRepository, InMemoryClientRepository>();
                    services.AddSingleton<INotificationCampaignRepository, InMemoryNotificationCampaignRepository>();
                    services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
                    services.AddSingleton<IPlanRepository, InMemoryPlanRepository>();
                    services.AddSingleton<ISensitiveDataAuditRepository, InMemorySensitiveDataAuditRepository>();
                    services.AddSingleton<IUserRepository, InMemoryUserRepository>();
                    services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
                });
            });
    }
}
