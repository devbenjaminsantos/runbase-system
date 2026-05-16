using RunBase.Application.Clients;
using RunBase.Domain.Clients;
using RunBase.Domain.Notifications;
using RunBase.Domain.Plans;

namespace RunBase.Application.Notifications;

public sealed class NotificationCampaignsService : INotificationCampaignsService
{
    private static readonly TimeSpan BillingUpcomingWindow = TimeSpan.FromDays(7);

    private readonly INotificationCampaignRepository _campaigns;
    private readonly IClientRepository _clients;

    public NotificationCampaignsService(
        INotificationCampaignRepository campaigns,
        IClientRepository clients)
    {
        _campaigns = campaigns;
        _clients = clients;
    }

    public async Task<IReadOnlyList<NotificationCampaignResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var campaigns = await _campaigns.ListAsync(cancellationToken);
        var clients = await _clients.ListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return campaigns
            .OrderByDescending(campaign => campaign.CreatedAt)
            .Select(campaign => ToResponse(campaign, clients, now))
            .ToList();
    }

    public async Task<NotificationCampaignResult<NotificationCampaignResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var campaign = await _campaigns.GetByIdAsync(id, cancellationToken);

        if (campaign is null)
        {
            return NotificationCampaignResult<NotificationCampaignResponse>.Failure(
                NotificationCampaignError.NotFound);
        }

        var clients = await _clients.ListAsync(cancellationToken);

        return NotificationCampaignResult<NotificationCampaignResponse>.Success(
            ToResponse(campaign, clients, DateTimeOffset.UtcNow));
    }

    public async Task<NotificationCampaignPreviewResponse> PreviewAsync(
        NotificationCampaignPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var clients = await _clients.ListAsync(cancellationToken);
        var count = CountAudience(
            clients,
            request.Type,
            request.TargetPlanStage,
            DateTimeOffset.UtcNow);

        return new NotificationCampaignPreviewResponse(
            request.Type,
            request.TargetPlanStage,
            count);
    }

    public async Task<NotificationCampaignResult<NotificationCampaignResponse>> CreateAsync(
        CreateNotificationCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var campaign = new NotificationCampaign(
            Guid.NewGuid(),
            request.Name,
            request.Type,
            request.ScheduledAt is null ? NotificationCampaignStatus.Draft : NotificationCampaignStatus.Scheduled,
            request.TargetPlanStage,
            request.ScheduledAt,
            now,
            now);

        await _campaigns.SaveAsync(campaign, cancellationToken);

        var clients = await _clients.ListAsync(cancellationToken);

        return NotificationCampaignResult<NotificationCampaignResponse>.Success(
            ToResponse(campaign, clients, now));
    }

    private static NotificationCampaignResponse ToResponse(
        NotificationCampaign campaign,
        IReadOnlyList<Client> clients,
        DateTimeOffset now)
    {
        return new NotificationCampaignResponse(
            campaign.Id,
            campaign.Name,
            campaign.Type,
            campaign.Status,
            campaign.TargetPlanStage,
            CountAudience(clients, campaign.Type, campaign.TargetPlanStage, now),
            campaign.ScheduledAt,
            campaign.CreatedAt,
            campaign.UpdatedAt);
    }

    private static int CountAudience(
        IReadOnlyList<Client> clients,
        NotificationCampaignType type,
        PlanStage? targetPlanStage,
        DateTimeOffset now)
    {
        return clients.Count(client => IsInAudience(client, type, targetPlanStage, now));
    }

    private static bool IsInAudience(
        Client client,
        NotificationCampaignType type,
        PlanStage? targetPlanStage,
        DateTimeOffset now)
    {
        if (client.Status != ClientStatus.Active)
        {
            return false;
        }

        if (targetPlanStage is not null && client.PlanStage != targetPlanStage)
        {
            return false;
        }

        return type switch
        {
            NotificationCampaignType.Promotion => client.PlanStage is PlanStage.Trial or PlanStage.Free,
            NotificationCampaignType.BillingUpcoming => IsBillingUpcoming(client, now),
            NotificationCampaignType.BillingOverdue => IsBillingOverdue(client, now),
            _ => false
        };
    }

    private static bool IsBillingUpcoming(Client client, DateTimeOffset now)
    {
        return client.PlanStage is PlanStage.Plus or PlanStage.Premium &&
            client.NextBillingAt is not null &&
            client.NextBillingAt >= now &&
            client.NextBillingAt <= now.Add(BillingUpcomingWindow);
    }

    private static bool IsBillingOverdue(Client client, DateTimeOffset now)
    {
        return client.PlanStage is PlanStage.Plus or PlanStage.Premium &&
            client.NextBillingAt is not null &&
            client.NextBillingAt < now;
    }
}
