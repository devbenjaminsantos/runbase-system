using RunBase.Domain.Plans;

namespace RunBase.Application.Tests.Plans;

public sealed class PlanTests
{
    [Theory]
    [InlineData(PlanStage.Trial)]
    [InlineData(PlanStage.Free)]
    public void Constructor_AllowsNonPaidStagesWithoutBillingDate(PlanStage stage)
    {
        var now = DateTimeOffset.UtcNow;

        var plan = new Plan(Guid.NewGuid(), stage, null, now, now);

        Assert.Equal(stage, plan.Stage);
        Assert.False(plan.HasBilling);
    }

    [Theory]
    [InlineData(PlanStage.Plus)]
    [InlineData(PlanStage.Premium)]
    public void Constructor_RequiresBillingDateForPaidStages(PlanStage stage)
    {
        var now = DateTimeOffset.UtcNow;

        var exception = Assert.Throws<ArgumentException>(
            () => new Plan(Guid.NewGuid(), stage, null, now, now));

        Assert.Equal("nextBillingAt", exception.ParamName);
    }

    [Theory]
    [InlineData(PlanStage.Plus)]
    [InlineData(PlanStage.Premium)]
    public void Constructor_AllowsPaidStagesWithBillingDate(PlanStage stage)
    {
        var now = DateTimeOffset.UtcNow;
        var nextBillingAt = now.AddMonths(1);

        var plan = new Plan(Guid.NewGuid(), stage, nextBillingAt, now, now);

        Assert.Equal(stage, plan.Stage);
        Assert.Equal(nextBillingAt, plan.NextBillingAt);
        Assert.True(plan.HasBilling);
    }

    [Fact]
    public void ChangeStage_UpdatesStageAndBillingDate()
    {
        var now = DateTimeOffset.UtcNow;
        var nextBillingAt = now.AddMonths(1);
        var plan = new Plan(Guid.NewGuid(), PlanStage.Free, null, now, now);

        plan.ChangeStage(PlanStage.Plus, nextBillingAt, now.AddMinutes(1));

        Assert.Equal(PlanStage.Plus, plan.Stage);
        Assert.Equal(nextBillingAt, plan.NextBillingAt);
        Assert.True(plan.UpdatedAt > now);
    }
}
