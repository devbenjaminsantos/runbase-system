using RunBase.Application.Notifications;
using RunBase.Domain.Notifications;

namespace RunBase.Infrastructure.Notifications;

public sealed class InMemoryNotificationCampaignRepository : INotificationCampaignRepository
{
    private readonly Dictionary<Guid, NotificationCampaign> _campaigns = new();

    public Task<IReadOnlyList<NotificationCampaign>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<NotificationCampaign>>(_campaigns.Values.ToList());
    }

    public Task<NotificationCampaign?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _campaigns.TryGetValue(id, out var campaign);

        return Task.FromResult(campaign);
    }

    public Task SaveAsync(
        NotificationCampaign campaign,
        CancellationToken cancellationToken = default)
    {
        _campaigns[campaign.Id] = campaign;

        return Task.CompletedTask;
    }
}
