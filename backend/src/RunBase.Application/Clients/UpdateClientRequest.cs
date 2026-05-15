using System.ComponentModel.DataAnnotations;
using RunBase.Domain.Clients;
using RunBase.Domain.Plans;

namespace RunBase.Application.Clients;

public sealed record UpdateClientRequest(
    [property: Required]
    [property: StringLength(160, MinimumLength = 2)]
    string Name,
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(254)]
    string Email,
    ClientStatus Status,
    PlanStage PlanStage,
    DateTimeOffset? NextBillingAt);
