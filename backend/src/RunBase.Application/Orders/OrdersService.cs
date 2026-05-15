using RunBase.Application.Clients;
using RunBase.Domain.Orders;

namespace RunBase.Application.Orders;

public sealed class OrdersService : IOrdersService
{
    private readonly IOrderRepository _orders;
    private readonly IClientRepository _clients;

    public OrdersService(
        IOrderRepository orders,
        IClientRepository clients)
    {
        _orders = orders;
        _clients = clients;
    }

    public async Task<IReadOnlyList<OrderResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await _orders.ListAsync(cancellationToken);

        return orders
            .OrderByDescending(order => order.CreatedAt)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<OrderResult<OrderResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);

        return order is null
            ? OrderResult<OrderResponse>.Failure(OrderError.NotFound)
            : OrderResult<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<OrderResult<OrderResponse>> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FinalAmount < 0)
        {
            return OrderResult<OrderResponse>.Failure(OrderError.InvalidAmount);
        }

        if (await _clients.GetByIdAsync(request.ClientId, cancellationToken) is null)
        {
            return OrderResult<OrderResponse>.Failure(OrderError.ClientNotFound);
        }

        var now = DateTimeOffset.UtcNow;
        var order = new Order(
            Guid.NewGuid(),
            request.ClientId,
            request.PlanStage,
            request.Status,
            request.FinalAmount,
            now,
            now);

        await _orders.SaveAsync(order, cancellationToken);

        return OrderResult<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<OrderResult<OrderResponse>> UpdateAsync(
        Guid id,
        UpdateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);

        if (order is null)
        {
            return OrderResult<OrderResponse>.Failure(OrderError.NotFound);
        }

        if (request.FinalAmount < 0)
        {
            return OrderResult<OrderResponse>.Failure(OrderError.InvalidAmount);
        }

        if (await _clients.GetByIdAsync(request.ClientId, cancellationToken) is null)
        {
            return OrderResult<OrderResponse>.Failure(OrderError.ClientNotFound);
        }

        try
        {
            order.Update(
                request.ClientId,
                request.PlanStage,
                request.Status,
                request.FinalAmount,
                DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return OrderResult<OrderResponse>.Failure(OrderError.InvalidStatusTransition);
        }

        await _orders.SaveAsync(order, cancellationToken);

        return OrderResult<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<OrderResult<OrderResponse>> UpdateStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);

        if (order is null)
        {
            return OrderResult<OrderResponse>.Failure(OrderError.NotFound);
        }

        try
        {
            order.ChangeStatus(request.Status, DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return OrderResult<OrderResponse>.Failure(OrderError.InvalidStatusTransition);
        }

        await _orders.SaveAsync(order, cancellationToken);

        return OrderResult<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<OrderResult<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);

        if (order is null)
        {
            return OrderResult<bool>.Failure(OrderError.NotFound);
        }

        await _orders.DeleteAsync(order, cancellationToken);

        return OrderResult<bool>.Success(true);
    }

    private static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.ClientId,
            order.PlanStage,
            order.Status,
            order.FinalAmount,
            order.CreatedAt,
            order.UpdatedAt);
    }
}
