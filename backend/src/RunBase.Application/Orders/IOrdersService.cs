namespace RunBase.Application.Orders;

public interface IOrdersService
{
    Task<IReadOnlyList<OrderResponse>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<OrderResult<OrderResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OrderResult<OrderResponse>> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderResult<OrderResponse>> UpdateAsync(
        Guid id,
        UpdateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderResult<OrderResponse>> UpdateStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderResult<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
