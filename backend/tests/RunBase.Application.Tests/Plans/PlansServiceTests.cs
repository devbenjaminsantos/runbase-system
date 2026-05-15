using RunBase.Application.Plans;
using RunBase.Domain.Plans;

namespace RunBase.Application.Tests.Plans;

public sealed class PlansServiceTests
{
    [Fact]
    public async Task CreateAsync_WithFreePlan_CreatesPlan()
    {
        var service = CreateService();

        var result = await service.CreateAsync(new CreatePlanRequest(
            "Free",
            PlanStage.Free,
            0,
            BillingCycle.None,
            true,
            null));

        Assert.True(result.Succeeded);
        Assert.Equal(PlanStage.Free, result.Value!.Stage);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithPaidPlanMissingConfiguration_ReturnsInvalidConfiguration()
    {
        var service = CreateService();

        var result = await service.CreateAsync(new CreatePlanRequest(
            "Plus",
            PlanStage.Plus,
            0,
            BillingCycle.None,
            true,
            null));

        Assert.False(result.Succeeded);
        Assert.Equal(PlanError.InvalidConfiguration, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatedStage_ReturnsStageAlreadyExists()
    {
        var service = CreateService(CreatePlan(PlanStage.Free));

        var result = await service.CreateAsync(new CreatePlanRequest(
            "Free Copy",
            PlanStage.Free,
            0,
            BillingCycle.None,
            true,
            null));

        Assert.False(result.Succeeded);
        Assert.Equal(PlanError.StageAlreadyExists, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesPlanCatalogFields()
    {
        var plan = CreatePlan(PlanStage.Free);
        var service = CreateService(plan);
        var nextBillingAt = DateTimeOffset.UtcNow.AddMonths(1);

        var result = await service.UpdateAsync(plan.Id, new UpdatePlanRequest(
            "Plus",
            PlanStage.Plus,
            19.90m,
            BillingCycle.Monthly,
            true,
            nextBillingAt));

        Assert.True(result.Succeeded);
        Assert.Equal("Plus", result.Value!.Name);
        Assert.Equal(PlanStage.Plus, result.Value.Stage);
        Assert.Equal(19.90m, result.Value.Price);
        Assert.Equal(BillingCycle.Monthly, result.Value.BillingCycle);
    }

    [Fact]
    public async Task SetActiveAsync_TogglesPlanActiveState()
    {
        var plan = CreatePlan(PlanStage.Free);
        var service = CreateService(plan);

        var result = await service.SetActiveAsync(plan.Id, false);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPlan()
    {
        var plan = CreatePlan(PlanStage.Free);
        var service = CreateService(plan);

        var delete = await service.DeleteAsync(plan.Id);
        var get = await service.GetByIdAsync(plan.Id);

        Assert.True(delete.Succeeded);
        Assert.False(get.Succeeded);
        Assert.Equal(PlanError.NotFound, get.Error);
    }

    private static PlansService CreateService(params Plan[] plans)
    {
        return new PlansService(new FakePlanRepository(plans));
    }

    private static Plan CreatePlan(PlanStage stage)
    {
        var now = DateTimeOffset.UtcNow;

        return new Plan(
            Guid.NewGuid(),
            stage.ToString(),
            stage,
            0,
            BillingCycle.None,
            true,
            null,
            now,
            now);
    }

    private sealed class FakePlanRepository : IPlanRepository
    {
        private readonly Dictionary<Guid, Plan> _plans;

        public FakePlanRepository(IReadOnlyList<Plan> plans)
        {
            _plans = plans.ToDictionary(plan => plan.Id);
        }

        public Task<IReadOnlyList<Plan>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Plan>>(_plans.Values.ToList());
        }

        public Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
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

        public Task SaveAsync(Plan plan, CancellationToken cancellationToken = default)
        {
            _plans[plan.Id] = plan;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Plan plan, CancellationToken cancellationToken = default)
        {
            _plans.Remove(plan.Id);

            return Task.CompletedTask;
        }
    }
}
