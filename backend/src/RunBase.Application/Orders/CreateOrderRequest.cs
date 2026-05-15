using System.ComponentModel.DataAnnotations;
using RunBase.Domain.Orders;
using RunBase.Domain.Plans;

namespace RunBase.Application.Orders;

public sealed record CreateOrderRequest(
    Guid ClientId,
    PlanStage PlanStage,
    OrderStatus Status,
    [property: Range(0, 1_000_000)]
    decimal FinalAmount);
