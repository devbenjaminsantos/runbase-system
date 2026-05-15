using RunBase.Domain.Plans;

namespace RunBase.Application.Plans;

public sealed record PlanResponse(
    Guid Id,
    string Name,
    PlanStage Stage,
    decimal Price,
    BillingCycle BillingCycle,
    bool IsActive,
    DateTimeOffset? NextBillingAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
