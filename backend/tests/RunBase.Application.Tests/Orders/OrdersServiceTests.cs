using RunBase.Application.Clients;
using RunBase.Application.Orders;
using RunBase.Domain;
using RunBase.Domain.Clients;
using RunBase.Domain.Orders;
using RunBase.Domain.Plans;

namespace RunBase.Application.Tests.Orders;

public sealed class OrdersServiceTests
{
    [Fact]
    public async Task CreateAsync_WithExistingClient_CreatesOrder()
    {
        var client = CreateClient();
        var service = CreateService(clients: [client]);

        var result = await service.CreateAsync(new CreateOrderRequest(
            client.Id,
            PlanStage.Plus,
            OrderStatus.Pending,
            19.90m));

        Assert.True(result.Succeeded);
        Assert.Equal(client.Id, result.Value!.ClientId);
        Assert.Equal(19.90m, result.Value.FinalAmount);
        Assert.Equal(OrderStatus.Pending, result.Value.Status);
    }

    [Fact]
    public async Task CreateAsync_WithMissingClient_ReturnsClientNotFound()
    {
        var service = CreateService();

        var result = await service.CreateAsync(new CreateOrderRequest(
            Guid.NewGuid(),
            PlanStage.Plus,
            OrderStatus.Pending,
            19.90m));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderError.ClientNotFound, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithNegativeAmount_ReturnsInvalidAmount()
    {
        var client = CreateClient();
        var service = CreateService(clients: [client]);

        var result = await service.CreateAsync(new CreateOrderRequest(
            client.Id,
            PlanStage.Plus,
            OrderStatus.Pending,
            -1));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderError.InvalidAmount, result.Error);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesOrderStatus()
    {
        var client = CreateClient();
        var order = CreateOrder(client.Id, OrderStatus.Pending);
        var service = CreateService([order], [client]);

        var result = await service.UpdateStatusAsync(
            order.Id,
            new UpdateOrderStatusRequest(OrderStatus.Processing));

        Assert.True(result.Succeeded);
        Assert.Equal(OrderStatus.Processing, result.Value!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOrderIsCompleted_ReturnsInvalidStatusTransition()
    {
        var client = CreateClient();
        var order = CreateOrder(client.Id, OrderStatus.Completed);
        var service = CreateService([order], [client]);

        var result = await service.UpdateStatusAsync(
            order.Id,
            new UpdateOrderStatusRequest(OrderStatus.Cancelled));

        Assert.False(result.Succeeded);
        Assert.Equal(OrderError.InvalidStatusTransition, result.Error);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOrder()
    {
        var client = CreateClient();
        var order = CreateOrder(client.Id, OrderStatus.Pending);
        var service = CreateService([order], [client]);

        var delete = await service.DeleteAsync(order.Id);
        var get = await service.GetByIdAsync(order.Id);

        Assert.True(delete.Succeeded);
        Assert.False(get.Succeeded);
        Assert.Equal(OrderError.NotFound, get.Error);
    }

    private static OrdersService CreateService(
        IReadOnlyList<Order>? orders = null,
        IReadOnlyList<Client>? clients = null)
    {
        return new OrdersService(
            new FakeOrderRepository(orders ?? []),
            new FakeClientRepository(clients ?? []));
    }

    private static Client CreateClient()
    {
        var now = DateTimeOffset.UtcNow;

        return new Client(
            Guid.NewGuid(),
            "Demo Client",
            "client@demo.runbase.local",
            ClientStatus.Active,
            PlanStage.Free,
            DataSource.Manual,
            null,
            now,
            now);
    }

    private static Order CreateOrder(
        Guid clientId,
        OrderStatus status)
    {
        var now = DateTimeOffset.UtcNow;

        return new Order(
            Guid.NewGuid(),
            clientId,
            PlanStage.Plus,
            status,
            19.90m,
            now,
            now);
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly Dictionary<Guid, Order> _orders;

        public FakeOrderRepository(IReadOnlyList<Order> orders)
        {
            _orders = orders.ToDictionary(order => order.Id);
        }

        public Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Order>>(_orders.Values.ToList());
        }

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _orders.TryGetValue(id, out var order);

            return Task.FromResult(order);
        }

        public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
        {
            _orders[order.Id] = order;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
        {
            _orders.Remove(order.Id);

            return Task.CompletedTask;
        }
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
