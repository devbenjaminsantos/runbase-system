using RunBase.Application.Clients;
using RunBase.Application.Notifications;
using RunBase.Domain;
using RunBase.Domain.Clients;
using RunBase.Domain.Notifications;
using RunBase.Domain.Plans;

namespace RunBase.Application.Tests.Notifications;

public sealed class NotificationCampaignsServiceTests
{
    [Fact]
    public async Task PreviewAsync_WithPromotion_CountsActiveTrialAndFreeClients()
    {
        var service = CreateService(
            CreateClient("trial@runbase.local", ClientStatus.Active, PlanStage.Trial, null),
            CreateClient("free@runbase.local", ClientStatus.Active, PlanStage.Free, null),
            CreateClient("plus@runbase.local", ClientStatus.Active, PlanStage.Plus, DateTimeOffset.UtcNow.AddDays(4)),
            CreateClient("inactive@runbase.local", ClientStatus.Inactive, PlanStage.Free, null));

        var result = await service.PreviewAsync(new NotificationCampaignPreviewRequest(
            NotificationCampaignType.Promotion));

        Assert.Equal(2, result.EstimatedAudienceCount);
    }

    [Fact]
    public async Task PreviewAsync_WithBillingUpcoming_CountsPaidClientsDueWithinSevenDays()
    {
        var service = CreateService(
            CreateClient("due@runbase.local", ClientStatus.Active, PlanStage.Plus, DateTimeOffset.UtcNow.AddDays(3)),
            CreateClient("premium@runbase.local", ClientStatus.Active, PlanStage.Premium, DateTimeOffset.UtcNow.AddDays(7)),
            CreateClient("later@runbase.local", ClientStatus.Active, PlanStage.Plus, DateTimeOffset.UtcNow.AddDays(10)),
            CreateClient("free@runbase.local", ClientStatus.Active, PlanStage.Free, null));

        var result = await service.PreviewAsync(new NotificationCampaignPreviewRequest(
            NotificationCampaignType.BillingUpcoming));

        Assert.Equal(2, result.EstimatedAudienceCount);
    }

    [Fact]
    public async Task PreviewAsync_WithBillingOverdue_CountsPaidClientsWithPastBillingDate()
    {
        var service = CreateService(
            CreateClient("overdue@runbase.local", ClientStatus.Active, PlanStage.Plus, DateTimeOffset.UtcNow.AddDays(-2)),
            CreateClient("future@runbase.local", ClientStatus.Active, PlanStage.Plus, DateTimeOffset.UtcNow.AddDays(2)),
            CreateClient("suspended@runbase.local", ClientStatus.Suspended, PlanStage.Premium, DateTimeOffset.UtcNow.AddDays(-4)));

        var result = await service.PreviewAsync(new NotificationCampaignPreviewRequest(
            NotificationCampaignType.BillingOverdue));

        Assert.Equal(1, result.EstimatedAudienceCount);
    }

    [Fact]
    public async Task CreateAsync_WithScheduledAt_PersistsScheduledCampaignWithAudienceEstimate()
    {
        var campaignRepository = new FakeNotificationCampaignRepository();
        var service = CreateService(
            campaignRepository,
            CreateClient("free@runbase.local", ClientStatus.Active, PlanStage.Free, null));
        var scheduledAt = DateTimeOffset.UtcNow.AddDays(1);

        var result = await service.CreateAsync(new CreateNotificationCampaignRequest(
            "Free upgrade offer",
            NotificationCampaignType.Promotion,
            PlanStage.Free,
            scheduledAt));

        var campaigns = await campaignRepository.ListAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(NotificationCampaignStatus.Scheduled, result.Value!.Status);
        Assert.Equal(1, result.Value.EstimatedAudienceCount);
        Assert.Single(campaigns);
    }

    private static NotificationCampaignsService CreateService(params Client[] clients)
    {
        return CreateService(new FakeNotificationCampaignRepository(), clients);
    }

    private static NotificationCampaignsService CreateService(
        INotificationCampaignRepository campaigns,
        params Client[] clients)
    {
        return new NotificationCampaignsService(
            campaigns,
            new FakeClientRepository(clients));
    }

    private static Client CreateClient(
        string email,
        ClientStatus status,
        PlanStage planStage,
        DateTimeOffset? nextBillingAt)
    {
        var now = DateTimeOffset.UtcNow;

        return new Client(
            Guid.NewGuid(),
            "Demo Client",
            email,
            status,
            planStage,
            DataSource.Manual,
            nextBillingAt,
            now,
            now);
    }

    private sealed class FakeClientRepository : IClientRepository
    {
        private readonly Dictionary<Guid, Client> _clients;

        public FakeClientRepository(IReadOnlyList<Client> clients)
        {
            _clients = clients.ToDictionary(client => client.Id);
        }

        public Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Client>>(_clients.Values.ToList());
        }

        public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _clients.TryGetValue(id, out var client);

            return Task.FromResult(client);
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? exceptClientId = null,
            CancellationToken cancellationToken = default)
        {
            var exists = _clients.Values.Any(client =>
                client.Id != exceptClientId &&
                string.Equals(client.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task SaveAsync(Client client, CancellationToken cancellationToken = default)
        {
            _clients[client.Id] = client;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Client client, CancellationToken cancellationToken = default)
        {
            _clients.Remove(client.Id);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotificationCampaignRepository : INotificationCampaignRepository
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
}
