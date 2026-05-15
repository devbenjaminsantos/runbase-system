namespace RunBase.Domain.Plans;

public sealed class Plan
{
    public Plan(
        Guid id,
        string name,
        PlanStage stage,
        decimal price,
        BillingCycle billingCycle,
        bool isActive,
        DateTimeOffset? nextBillingAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        EnsureBillingDate(stage, nextBillingAt);
        EnsurePrice(stage, price);
        EnsureBillingCycle(stage, billingCycle);

        Id = id;
        Name = name;
        Stage = stage;
        Price = price;
        BillingCycle = billingCycle;
        IsActive = isActive;
        NextBillingAt = nextBillingAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public PlanStage Stage { get; private set; }

    public decimal Price { get; private set; }

    public BillingCycle BillingCycle { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? NextBillingAt { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool HasBilling => NextBillingAt is not null;

    public void Update(
        string name,
        PlanStage stage,
        decimal price,
        BillingCycle billingCycle,
        bool isActive,
        DateTimeOffset? nextBillingAt,
        DateTimeOffset updatedAt)
    {
        EnsureBillingDate(stage, nextBillingAt);
        EnsurePrice(stage, price);
        EnsureBillingCycle(stage, billingCycle);

        Name = name;
        Stage = stage;
        Price = price;
        BillingCycle = billingCycle;
        IsActive = isActive;
        NextBillingAt = nextBillingAt;
        UpdatedAt = updatedAt;
    }

    public void SetActive(bool isActive, DateTimeOffset updatedAt)
    {
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }

    public static bool RequiresBillingDate(PlanStage stage)
    {
        return stage is PlanStage.Plus or PlanStage.Premium;
    }

    public static bool RequiresPaidConfiguration(PlanStage stage)
    {
        return stage is PlanStage.Plus or PlanStage.Premium;
    }

    private static void EnsureBillingDate(
        PlanStage stage,
        DateTimeOffset? nextBillingAt)
    {
        if (RequiresBillingDate(stage) && nextBillingAt is null)
        {
            throw new ArgumentException("Plus and Premium plans require a next billing date.", nameof(nextBillingAt));
        }
    }

    private static void EnsurePrice(
        PlanStage stage,
        decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentException("Plan price cannot be negative.", nameof(price));
        }

        if (RequiresPaidConfiguration(stage) && price <= 0)
        {
            throw new ArgumentException("Plus and Premium plans require a positive price.", nameof(price));
        }
    }

    private static void EnsureBillingCycle(
        PlanStage stage,
        BillingCycle billingCycle)
    {
        if (RequiresPaidConfiguration(stage) && billingCycle == BillingCycle.None)
        {
            throw new ArgumentException("Plus and Premium plans require a billing cycle.", nameof(billingCycle));
        }
    }
}
