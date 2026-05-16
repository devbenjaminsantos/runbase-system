using RunBase.Domain.Notifications;

namespace RunBase.Application.Notifications;

public interface INotificationCampaignRepository
{
    Task<IReadOnlyList<NotificationCampaign>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<NotificationCampaign?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        NotificationCampaign campaign,
        CancellationToken cancellationToken = default);
}
