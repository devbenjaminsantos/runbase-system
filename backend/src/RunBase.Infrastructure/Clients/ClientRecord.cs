using RunBase.Domain;
using RunBase.Domain.Clients;
using RunBase.Domain.Plans;

namespace RunBase.Infrastructure.Clients;

public sealed class ClientRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string EmailCipherText { get; set; } = string.Empty;

    public string EmailLookupHash { get; set; } = string.Empty;

    public ClientStatus Status { get; set; }

    public PlanStage PlanStage { get; set; }

    public DataSource DataSource { get; set; }

    public DateTimeOffset? NextBillingAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
