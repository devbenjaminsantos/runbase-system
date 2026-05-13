using RunBase.Application.Health;

namespace RunBase.Application.Tests.Health;

public sealed class HealthStatusServiceTests
{
    [Fact]
    public void GetStatus_ReturnsHealthyRunBaseApiStatus()
    {
        var service = new HealthStatusService();

        var status = service.GetStatus();

        Assert.Equal("RunBase.Api", status.Service);
        Assert.Equal("Healthy", status.Status);
        Assert.True(status.TimestampUtc <= DateTimeOffset.UtcNow);
    }
}
