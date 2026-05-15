using RunBase.Domain.Orders;
using RunBase.Domain.Plans;

namespace RunBase.Application.Orders;

public sealed record CreateOrderRequest(
    Guid ClientId,
    PlanStage PlanStage,
    OrderStatus Status,
    decimal FinalAmount);
