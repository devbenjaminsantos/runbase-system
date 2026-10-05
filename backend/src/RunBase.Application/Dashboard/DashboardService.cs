using RunBase.Application.Clients;
using RunBase.Application.Orders;
using RunBase.Application.Plans;
using RunBase.Domain.Clients;
using RunBase.Domain.Orders;
using RunBase.Domain.Plans;

namespace RunBase.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private static readonly PlanStage[] PlanStages = Enum.GetValues<PlanStage>();
    private readonly IClientRepository _clients;
    private readonly IOrderRepository _orders;
    private readonly IPlanRepository _plans;

    public DashboardService(
        IClientRepository clients,
        IOrderRepository orders,
        IPlanRepository plans)
    {
        _clients = clients;
        _orders = orders;
        _plans = plans;
    }

    public async Task<DashboardResponse> GetAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var clients = await _clients.ListAsync(cancellationToken);
        var orders = await _orders.ListAsync(cancellationToken);
        var plans = await _plans.ListAsync(cancellationToken);
        var activeClients = clients
            .Where(client => client.Status == ClientStatus.Active)
            .ToList();
        var startOfToday = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var endOfUpcomingWindow = startOfToday.AddDays(7);
        var clientNames = clients.ToDictionary(client => client.Id, client => client.Name);
        var recentOrders = orders
            .OrderByDescending(order => order.UpdatedAt)
            .Take(5)
            .Select(order => new DashboardRecentOrderResponse(
                order.Id,
                clientNames.GetValueOrDefault(order.ClientId),
                order.PlanStage,
                order.Status,
                order.FinalAmount,
                order.UpdatedAt))
            .ToList();
        var planDistribution = PlanStages
            .Select(stage => new DashboardPlanDistributionResponse(
                stage,
                activeClients.Count(client => client.PlanStage == stage)))
            .ToList();

        return new DashboardResponse(
            activeClients.Count,
            clients.Count,
            plans.Count(plan => plan.IsActive),
            plans.Count,
            orders.Count(order => order.Status is OrderStatus.Pending or OrderStatus.Processing),
            orders.Count,
            orders
                .Where(order => order.Status == OrderStatus.Completed)
                .Sum(order => order.FinalAmount),
            activeClients.Count(client => client.NextBillingAt < startOfToday),
            activeClients.Count(client =>
                client.NextBillingAt >= startOfToday &&
                client.NextBillingAt <= endOfUpcomingWindow),
            recentOrders,
            planDistribution);
    }
}
