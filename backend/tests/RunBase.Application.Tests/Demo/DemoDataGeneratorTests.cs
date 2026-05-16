using RunBase.Application.Demo;
using RunBase.Domain;
using RunBase.Domain.Plans;

namespace RunBase.Application.Tests.Demo;

public sealed class DemoDataGeneratorTests
{
    [Fact]
    public void GenerateClients_CreatesRequestedAmount()
    {
        var generator = new DemoDataGenerator();

        var clients = generator.GenerateClients(12, DateTimeOffset.Parse("2026-05-15T00:00:00Z"));

        Assert.Equal(12, clients.Count);
    }

    [Fact]
    public void GenerateClients_UsesSyntheticEmailDomain()
    {
        var generator = new DemoDataGenerator();

        var clients = generator.GenerateClients(5, DateTimeOffset.Parse("2026-05-15T00:00:00Z"));

        Assert.All(clients, client => Assert.EndsWith("@demo.runbase.local", client.Email, StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateClients_AddsBillingDatesOnlyForPaidPlans()
    {
        var generator = new DemoDataGenerator();

        var clients = generator.GenerateClients(8, DateTimeOffset.Parse("2026-05-15T00:00:00Z"));

        Assert.All(clients, client =>
        {
            if (client.PlanStage is PlanStage.Plus or PlanStage.Premium)
            {
                Assert.NotNull(client.NextBillingAt);
            }
            else
            {
                Assert.Null(client.NextBillingAt);
            }
        });
    }

    [Fact]
    public void GenerateClients_MarksAllClientsAsDemoSource()
    {
        var generator = new DemoDataGenerator();

        var clients = generator.GenerateClients(5, DateTimeOffset.Parse("2026-05-15T00:00:00Z"));

        Assert.All(clients, client => Assert.Equal(DataSource.Demo, client.DataSource));
    }
}
