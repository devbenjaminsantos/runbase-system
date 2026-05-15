using RunBase.Domain.Clients;
using RunBase.Domain.Plans;

namespace RunBase.Application.Clients;

public sealed record UpdateClientRequest(
    string Name,
    string Email,
    ClientStatus Status,
    PlanStage PlanStage,
    DateTimeOffset? NextBillingAt);
