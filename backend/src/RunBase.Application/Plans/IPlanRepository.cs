using RunBase.Domain.Plans;

namespace RunBase.Application.Plans;

public interface IPlanRepository
{
    Task<IReadOnlyList<Plan>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Plan?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> StageExistsAsync(
        PlanStage stage,
        Guid? exceptPlanId = null,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Plan plan,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Plan plan,
        CancellationToken cancellationToken = default);
}
