namespace RunBase.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
