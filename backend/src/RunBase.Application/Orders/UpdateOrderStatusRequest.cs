using RunBase.Domain.Orders;

namespace RunBase.Application.Orders;

public sealed record UpdateOrderStatusRequest(
    OrderStatus Status);
