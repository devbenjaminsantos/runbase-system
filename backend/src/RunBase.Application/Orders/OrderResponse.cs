using RunBase.Domain.Orders;
using RunBase.Domain.Plans;

namespace RunBase.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    Guid ClientId,
    PlanStage PlanStage,
    OrderStatus Status,
    decimal FinalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
