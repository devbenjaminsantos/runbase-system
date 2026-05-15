using System.Collections.Concurrent;
using RunBase.Application.Clients;
using RunBase.Domain.Clients;

namespace RunBase.Infrastructure.Clients;

public sealed class InMemoryClientRepository : IClientRepository
{
    private readonly ConcurrentDictionary<Guid, Client> _clients = new();

    public Task<IReadOnlyList<Client>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Client>>(_clients.Values.ToList());
    }

    public Task<Client?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
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

    public Task SaveAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        _clients[client.Id] = client;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        _clients.TryRemove(client.Id, out _);

        return Task.CompletedTask;
    }
}
