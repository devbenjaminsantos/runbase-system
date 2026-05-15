namespace RunBase.Application.Orders;

public sealed class OrderResult<T>
{
    private OrderResult(T? value, OrderError error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public OrderError Error { get; }

    public bool Succeeded => Error == OrderError.None;

    public static OrderResult<T> Success(T value)
    {
        return new OrderResult<T>(value, OrderError.None);
    }

    public static OrderResult<T> Failure(OrderError error)
    {
        return new OrderResult<T>(default, error);
    }
}
