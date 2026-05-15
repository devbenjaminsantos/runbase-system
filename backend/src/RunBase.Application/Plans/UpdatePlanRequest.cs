using RunBase.Domain.Plans;

namespace RunBase.Application.Plans;

public sealed record UpdatePlanRequest(
    string Name,
    PlanStage Stage,
    decimal Price,
    BillingCycle BillingCycle,
    bool IsActive,
    DateTimeOffset? NextBillingAt);
