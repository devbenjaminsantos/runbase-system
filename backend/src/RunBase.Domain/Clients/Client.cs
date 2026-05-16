using RunBase.Domain.Plans;

namespace RunBase.Domain.Clients;

public sealed class Client
{
    public Client(
        Guid id,
        string name,
        string email,
        ClientStatus status,
        PlanStage planStage,
        DataSource dataSource,
        DateTimeOffset? nextBillingAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        EnsureBillingDate(planStage, nextBillingAt);

        Id = id;
        Name = name;
        Email = email;
        Status = status;
        PlanStage = planStage;
        DataSource = dataSource;
        NextBillingAt = nextBillingAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public ClientStatus Status { get; private set; }

    public PlanStage PlanStage { get; private set; }

    public DataSource DataSource { get; private set; }

    public DateTimeOffset? NextBillingAt { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(
        string name,
        string email,
        ClientStatus status,
        PlanStage planStage,
        DataSource dataSource,
        DateTimeOffset? nextBillingAt,
        DateTimeOffset updatedAt)
    {
        EnsureBillingDate(planStage, nextBillingAt);

        Name = name;
        Email = email;
        Status = status;
        PlanStage = planStage;
        DataSource = dataSource;
        NextBillingAt = nextBillingAt;
        UpdatedAt = updatedAt;
    }

    private static void EnsureBillingDate(
        PlanStage planStage,
        DateTimeOffset? nextBillingAt)
    {
        if (Plan.RequiresBillingDate(planStage) && nextBillingAt is null)
        {
            throw new ArgumentException("Plus and Premium clients require a next billing date.", nameof(nextBillingAt));
        }
    }
}
