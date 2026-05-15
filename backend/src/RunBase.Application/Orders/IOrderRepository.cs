using RunBase.Domain.Orders;

namespace RunBase.Application.Orders;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
