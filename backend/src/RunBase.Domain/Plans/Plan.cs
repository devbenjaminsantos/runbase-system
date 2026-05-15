namespace RunBase.Domain.Plans;

public sealed class Plan
{
    public Plan(
        Guid id,
        PlanStage stage,
        DateTimeOffset? nextBillingAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (RequiresBillingDate(stage) && nextBillingAt is null)
        {
            throw new ArgumentException("Plus and Premium plans require a next billing date.", nameof(nextBillingAt));
        }

        Id = id;
        Stage = stage;
        NextBillingAt = nextBillingAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public PlanStage Stage { get; private set; }

    public DateTimeOffset? NextBillingAt { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool HasBilling => NextBillingAt is not null;

    public void ChangeStage(
        PlanStage stage,
        DateTimeOffset? nextBillingAt,
        DateTimeOffset updatedAt)
    {
        if (RequiresBillingDate(stage) && nextBillingAt is null)
        {
            throw new ArgumentException("Plus and Premium plans require a next billing date.", nameof(nextBillingAt));
        }

        Stage = stage;
        NextBillingAt = nextBillingAt;
        UpdatedAt = updatedAt;
    }

    public static bool RequiresBillingDate(PlanStage stage)
    {
        return stage is PlanStage.Plus or PlanStage.Premium;
    }
}
