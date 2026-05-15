using RunBase.Domain.Plans;

namespace RunBase.Domain.Orders;

public sealed class Order
{
    public Order(
        Guid id,
        Guid clientId,
        PlanStage planStage,
        OrderStatus status,
        decimal finalAmount,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        EnsureFinalAmount(finalAmount);

        Id = id;
        ClientId = clientId;
        PlanStage = planStage;
        Status = status;
        FinalAmount = finalAmount;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public Guid ClientId { get; private set; }

    public PlanStage PlanStage { get; private set; }

    public OrderStatus Status { get; private set; }

    public decimal FinalAmount { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(
        Guid clientId,
        PlanStage planStage,
        OrderStatus status,
        decimal finalAmount,
        DateTimeOffset updatedAt)
    {
        EnsureFinalAmount(finalAmount);
        EnsureStatusTransition(status);

        ClientId = clientId;
        PlanStage = planStage;
        Status = status;
        FinalAmount = finalAmount;
        UpdatedAt = updatedAt;
    }

    public void ChangeStatus(
        OrderStatus status,
        DateTimeOffset updatedAt)
    {
        EnsureStatusTransition(status);

        Status = status;
        UpdatedAt = updatedAt;
    }

    private static void EnsureFinalAmount(decimal finalAmount)
    {
        if (finalAmount < 0)
        {
            throw new ArgumentException("Order final amount cannot be negative.", nameof(finalAmount));
        }
    }

    private void EnsureStatusTransition(OrderStatus nextStatus)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled &&
            nextStatus != Status)
        {
            throw new InvalidOperationException("Completed or cancelled orders cannot change status.");
        }
    }
}
