using System.ComponentModel.DataAnnotations;
using RunBase.Domain.Plans;

namespace RunBase.Application.Plans;

public sealed record CreatePlanRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 2)]
    string Name,
    PlanStage Stage,
    [property: Range(0, 1_000_000)]
    decimal Price,
    BillingCycle BillingCycle,
    bool IsActive,
    DateTimeOffset? NextBillingAt);
