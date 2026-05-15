using RunBase.Domain.Clients;
using RunBase.Domain.Plans;

namespace RunBase.Application.Demo;

public sealed class DemoDataGenerator : IDemoDataGenerator
{
    private static readonly string[] CompanyNames =
    [
        "Northwind Labs",
        "Contoso Ops",
        "Blue Orbit",
        "Fabrikam Care",
        "Vertex Studio",
        "Aster Cloud",
        "Lumina Works",
        "Nexa Field"
    ];

    public IReadOnlyList<DemoClientResponse> GenerateClients(
        int count,
        DateTimeOffset referenceDateUtc)
    {
        if (count <= 0)
        {
            return [];
        }

        return Enumerable.Range(1, count)
            .Select(index => CreateClient(index, referenceDateUtc))
            .ToList();
    }

    private static DemoClientResponse CreateClient(
        int index,
        DateTimeOffset referenceDateUtc)
    {
        var planStage = GetPlanStage(index);
        var status = GetStatus(index);
        DateTimeOffset? nextBillingAt = Plan.RequiresBillingDate(planStage)
            ? new DateTimeOffset(referenceDateUtc.Date, TimeSpan.Zero)
                .AddDays((index % 12) - 4)
                .AddHours(12)
            : null;
        var name = $"{CompanyNames[(index - 1) % CompanyNames.Length]} {index:000}";

        return new DemoClientResponse(
            name,
            $"client{index:000}@demo.runbase.local",
            status,
            planStage,
            nextBillingAt);
    }

    private static PlanStage GetPlanStage(int index)
    {
        return (index % 4) switch
        {
            0 => PlanStage.Premium,
            1 => PlanStage.Trial,
            2 => PlanStage.Free,
            _ => PlanStage.Plus
        };
    }

    private static ClientStatus GetStatus(int index)
    {
        return (index % 10) switch
        {
            0 => ClientStatus.Suspended,
            5 => ClientStatus.Inactive,
            _ => ClientStatus.Active
        };
    }
}
