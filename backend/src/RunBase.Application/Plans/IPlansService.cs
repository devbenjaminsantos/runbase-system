namespace RunBase.Application.Plans;

public interface IPlansService
{
    Task<IReadOnlyList<PlanResponse>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<PlanResult<PlanResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PlanResult<PlanResponse>> CreateAsync(
        CreatePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<PlanResult<PlanResponse>> UpdateAsync(
        Guid id,
        UpdatePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<PlanResult<PlanResponse>> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<PlanResult<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
