using RunBase.Domain;
using RunBase.Domain.Clients;
using RunBase.Domain.Plans;

namespace RunBase.Application.Demo;

public sealed record DemoClientResponse(
    string Name,
    string Email,
    ClientStatus Status,
    PlanStage PlanStage,
    DataSource DataSource,
    DateTimeOffset? NextBillingAt);
