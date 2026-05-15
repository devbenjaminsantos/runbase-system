using RunBase.Domain.Plans;

namespace RunBase.Application.Plans;

public sealed class PlansService : IPlansService
{
    private readonly IPlanRepository _plans;

    public PlansService(IPlanRepository plans)
    {
        _plans = plans;
    }

    public async Task<IReadOnlyList<PlanResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await _plans.ListAsync(cancellationToken);

        return plans
            .OrderBy(plan => plan.Stage)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<PlanResult<PlanResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken);

        return plan is null
            ? PlanResult<PlanResponse>.Failure(PlanError.NotFound)
            : PlanResult<PlanResponse>.Success(ToResponse(plan));
    }

    public async Task<PlanResult<PlanResponse>> CreateAsync(
        CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidConfiguration(request.Stage, request.Price, request.BillingCycle, request.NextBillingAt))
        {
            return PlanResult<PlanResponse>.Failure(PlanError.InvalidConfiguration);
        }

        if (await _plans.StageExistsAsync(request.Stage, cancellationToken: cancellationToken))
        {
            return PlanResult<PlanResponse>.Failure(PlanError.StageAlreadyExists);
        }

        var now = DateTimeOffset.UtcNow;
        var plan = new Plan(
            Guid.NewGuid(),
            request.Name,
            request.Stage,
            request.Price,
            request.BillingCycle,
            request.IsActive,
            request.NextBillingAt,
            now,
            now);

        await _plans.SaveAsync(plan, cancellationToken);

        return PlanResult<PlanResponse>.Success(ToResponse(plan));
    }

    public async Task<PlanResult<PlanResponse>> UpdateAsync(
        Guid id,
        UpdatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken);

        if (plan is null)
        {
            return PlanResult<PlanResponse>.Failure(PlanError.NotFound);
        }

        if (!IsValidConfiguration(request.Stage, request.Price, request.BillingCycle, request.NextBillingAt))
        {
            return PlanResult<PlanResponse>.Failure(PlanError.InvalidConfiguration);
        }

        if (await _plans.StageExistsAsync(request.Stage, id, cancellationToken))
        {
            return PlanResult<PlanResponse>.Failure(PlanError.StageAlreadyExists);
        }

        plan.Update(
            request.Name,
            request.Stage,
            request.Price,
            request.BillingCycle,
            request.IsActive,
            request.NextBillingAt,
            DateTimeOffset.UtcNow);

        await _plans.SaveAsync(plan, cancellationToken);

        return PlanResult<PlanResponse>.Success(ToResponse(plan));
    }

    public async Task<PlanResult<PlanResponse>> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken);

        if (plan is null)
        {
            return PlanResult<PlanResponse>.Failure(PlanError.NotFound);
        }

        plan.SetActive(isActive, DateTimeOffset.UtcNow);
        await _plans.SaveAsync(plan, cancellationToken);

        return PlanResult<PlanResponse>.Success(ToResponse(plan));
    }

    public async Task<PlanResult<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(id, cancellationToken);

        if (plan is null)
        {
            return PlanResult<bool>.Failure(PlanError.NotFound);
        }

        await _plans.DeleteAsync(plan, cancellationToken);

        return PlanResult<bool>.Success(true);
    }

    private static bool IsValidConfiguration(
        PlanStage stage,
        decimal price,
        BillingCycle billingCycle,
        DateTimeOffset? nextBillingAt)
    {
        if (price < 0)
        {
            return false;
        }

        if (!Plan.RequiresPaidConfiguration(stage))
        {
            return true;
        }

        return price > 0 &&
            billingCycle != BillingCycle.None &&
            nextBillingAt is not null;
    }

    private static PlanResponse ToResponse(Plan plan)
    {
        return new PlanResponse(
            plan.Id,
            plan.Name,
            plan.Stage,
            plan.Price,
            plan.BillingCycle,
            plan.IsActive,
            plan.NextBillingAt,
            plan.CreatedAt,
            plan.UpdatedAt);
    }
}
