using RunBase.Domain;

namespace RunBase.Application.Health;

public sealed class HealthStatusService : IHealthStatusService
{
    public SystemStatus GetStatus()
    {
        return new SystemStatus(
            Service: "RunBase.Api",
            Status: "Healthy",
            TimestampUtc: DateTimeOffset.UtcNow);
    }
}
