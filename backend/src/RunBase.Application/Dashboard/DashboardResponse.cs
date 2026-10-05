using RunBase.Domain.Orders;
using RunBase.Domain.Plans;

namespace RunBase.Application.Dashboard;

public sealed record DashboardResponse(
    int ActiveClientCount,
    int TotalClientCount,
    int ActivePlanCount,
    int TotalPlanCount,
    int OpenOrderCount,
    int TotalOrderCount,
    decimal CompletedRevenue,
    int OverdueBillingCount,
    int UpcomingBillingCount,
    IReadOnlyList<DashboardRecentOrderResponse> RecentOrders,
    IReadOnlyList<DashboardPlanDistributionResponse> PlanDistribution);

public sealed record DashboardRecentOrderResponse(
    Guid Id,
    string? ClientName,
    PlanStage PlanStage,
    OrderStatus Status,
    decimal FinalAmount,
    DateTimeOffset UpdatedAt);

public sealed record DashboardPlanDistributionResponse(
    PlanStage Stage,
    int Count);
