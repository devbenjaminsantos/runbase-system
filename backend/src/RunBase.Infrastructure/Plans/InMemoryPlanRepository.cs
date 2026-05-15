using System.Collections.Concurrent;
using RunBase.Application.Plans;
using RunBase.Domain.Plans;

namespace RunBase.Infrastructure.Plans;

public sealed class InMemoryPlanRepository : IPlanRepository
{
    private readonly ConcurrentDictionary<Guid, Plan> _plans = new();

    public Task<IReadOnlyList<Plan>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Plan>>(_plans.Values.ToList());
    }

    public Task<Plan?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _plans.TryGetValue(id, out var plan);

        return Task.FromResult(plan);
    }

    public Task<bool> StageExistsAsync(
        PlanStage stage,
        Guid? exceptPlanId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = _plans.Values.Any(plan =>
            plan.Id != exceptPlanId &&
            plan.Stage == stage);

        return Task.FromResult(exists);
    }

    public Task SaveAsync(
        Plan plan,
        CancellationToken cancellationToken = default)
    {
        _plans[plan.Id] = plan;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Plan plan,
        CancellationToken cancellationToken = default)
    {
        _plans.TryRemove(plan.Id, out _);

        return Task.CompletedTask;
    }
}
