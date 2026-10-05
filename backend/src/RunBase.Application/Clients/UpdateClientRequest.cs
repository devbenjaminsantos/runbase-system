using System.ComponentModel.DataAnnotations;
using RunBase.Domain;
using RunBase.Domain.Clients;
using RunBase.Domain.Plans;

namespace RunBase.Application.Clients;

public sealed record UpdateClientRequest(
    [property: Required]
    [property: StringLength(160, MinimumLength = 2)]
    string Name,
    ClientStatus Status,
    PlanStage PlanStage,
    DataSource DataSource,
    DateTimeOffset? NextBillingAt);
