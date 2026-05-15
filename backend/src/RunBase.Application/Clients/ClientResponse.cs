using RunBase.Domain.Clients;
using RunBase.Domain.Plans;

namespace RunBase.Application.Clients;

public sealed record ClientResponse(
    Guid Id,
    string Name,
    string MaskedEmail,
    ClientStatus Status,
    PlanStage PlanStage,
    DateTimeOffset? NextBillingAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
