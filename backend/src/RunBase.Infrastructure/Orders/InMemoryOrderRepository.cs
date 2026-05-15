using System.Collections.Concurrent;
using RunBase.Application.Orders;
using RunBase.Domain.Orders;

namespace RunBase.Infrastructure.Orders;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Order>>(_orders.Values.ToList());
    }

    public Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(id, out var order);

        return Task.FromResult(order);
    }

    public Task SaveAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        _orders.TryRemove(order.Id, out _);

        return Task.CompletedTask;
    }
}
