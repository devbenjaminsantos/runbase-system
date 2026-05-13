using RunBase.Domain;

namespace RunBase.Application.Health;

public interface IHealthStatusService
{
    SystemStatus GetStatus();
}
