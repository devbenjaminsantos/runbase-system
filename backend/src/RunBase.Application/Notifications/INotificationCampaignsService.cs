namespace RunBase.Application.Notifications;

public interface INotificationCampaignsService
{
    Task<IReadOnlyList<NotificationCampaignResponse>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<NotificationCampaignResult<NotificationCampaignResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<NotificationCampaignPreviewResponse> PreviewAsync(
        NotificationCampaignPreviewRequest request,
        CancellationToken cancellationToken = default);

    Task<NotificationCampaignResult<NotificationCampaignResponse>> CreateAsync(
        CreateNotificationCampaignRequest request,
        CancellationToken cancellationToken = default);
}
