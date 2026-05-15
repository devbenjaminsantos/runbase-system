using RunBase.Application.Clients;
using RunBase.Application.Security;
using RunBase.Domain.Clients;
using RunBase.Domain.Plans;

namespace RunBase.Application.Tests.Clients;

public sealed class ClientsServiceTests
{
    [Fact]
    public async Task CreateAsync_WithFreePlan_CreatesClientWithoutBillingDate()
    {
        var service = CreateService();

        var result = await service.CreateAsync(new CreateClientRequest(
            "Acme Demo",
            "acme@demo.runbase.local",
            ClientStatus.Active,
            PlanStage.Free,
            null));

        Assert.True(result.Succeeded);
        Assert.Equal(PlanStage.Free, result.Value!.PlanStage);
        Assert.Equal("ac***@demo.runbase.local", result.Value.MaskedEmail);
        Assert.Null(result.Value.NextBillingAt);
    }

    [Theory]
    [InlineData(PlanStage.Plus)]
    [InlineData(PlanStage.Premium)]
    public async Task CreateAsync_WithPaidPlanWithoutBillingDate_ReturnsBillingDateRequired(
        PlanStage planStage)
    {
        var service = CreateService();

        var result = await service.CreateAsync(new CreateClientRequest(
            "Paid Demo",
            "paid@demo.runbase.local",
            ClientStatus.Active,
            planStage,
            null));

        Assert.False(result.Succeeded);
        Assert.Equal(ClientError.BillingDateRequired, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatedEmail_ReturnsEmailAlreadyExists()
    {
        var service = CreateService(CreateClient("acme@demo.runbase.local"));

        var result = await service.CreateAsync(new CreateClientRequest(
            "Acme Copy",
            "ACME@demo.runbase.local",
            ClientStatus.Active,
            PlanStage.Free,
            null));

        Assert.False(result.Succeeded);
        Assert.Equal(ClientError.EmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesStatusPlanAndBillingDate()
    {
        var client = CreateClient("acme@demo.runbase.local");
        var service = CreateService(client);
        var nextBillingAt = DateTimeOffset.UtcNow.AddMonths(1);

        var result = await service.UpdateAsync(client.Id, new UpdateClientRequest(
            "Acme Premium",
            "premium@demo.runbase.local",
            ClientStatus.Suspended,
            PlanStage.Premium,
            nextBillingAt));

        Assert.True(result.Succeeded);
        Assert.Equal(ClientStatus.Suspended, result.Value!.Status);
        Assert.Equal(PlanStage.Premium, result.Value.PlanStage);
        Assert.Equal(nextBillingAt, result.Value.NextBillingAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesClient()
    {
        var client = CreateClient("acme@demo.runbase.local");
        var service = CreateService(client);

        var delete = await service.DeleteAsync(client.Id);
        var get = await service.GetByIdAsync(client.Id);

        Assert.True(delete.Succeeded);
        Assert.False(get.Succeeded);
        Assert.Equal(ClientError.NotFound, get.Error);
    }

    private static ClientsService CreateService(params Client[] clients)
    {
        return new ClientsService(
            new FakeClientRepository(clients),
            new SensitiveDataMasker());
    }

    private static Client CreateClient(string email)
    {
        var now = DateTimeOffset.UtcNow;

        return new Client(
            Guid.NewGuid(),
            "Demo Client",
            email,
            ClientStatus.Active,
            PlanStage.Free,
            null,
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
}
