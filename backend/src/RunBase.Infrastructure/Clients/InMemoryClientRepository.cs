using System.Collections.Concurrent;
using RunBase.Application.Clients;
using RunBase.Application.Security;
using RunBase.Domain.Clients;

namespace RunBase.Infrastructure.Clients;

public sealed class InMemoryClientRepository : IClientRepository
{
    private readonly ConcurrentDictionary<Guid, StoredClient> _clients = new();
    private readonly ISensitiveDataProtector _sensitiveDataProtector;

    public InMemoryClientRepository(ISensitiveDataProtector sensitiveDataProtector)
    {
        _sensitiveDataProtector = sensitiveDataProtector;
    }

    public Task<IReadOnlyList<Client>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var clients = _clients.Values
            .Select(ToClient)
            .ToList();

        return Task.FromResult<IReadOnlyList<Client>>(clients);
    }

    public Task<Client?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _clients.TryGetValue(id, out var storedClient);
        var client = storedClient is null
            ? null
            : ToClient(storedClient);

        return Task.FromResult(client);
    }

    public Task<bool> EmailExistsAsync(
        string email,
        Guid? exceptClientId = null,
        CancellationToken cancellationToken = default)
    {
        var lookupHash = _sensitiveDataProtector.CreateLookupHash(email);
        var exists = _clients.Values.Any(client =>
            client.Id != exceptClientId &&
            client.EmailLookupHash == lookupHash);

        return Task.FromResult(exists);
    }

    public Task SaveAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        var protectedEmail = _sensitiveDataProtector.Protect(client.Email);
        _clients[client.Id] = new StoredClient(
            client.Id,
            client.Name,
            protectedEmail.CipherText,
            protectedEmail.LookupHash,
            client.Status,
            client.PlanStage,
            client.NextBillingAt,
            client.CreatedAt,
            client.UpdatedAt);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        _clients.TryRemove(client.Id, out _);

        return Task.CompletedTask;
    }

    private Client ToClient(StoredClient storedClient)
    {
        return new Client(
            storedClient.Id,
            storedClient.Name,
            _sensitiveDataProtector.Unprotect(storedClient.EmailCipherText),
            storedClient.Status,
            storedClient.PlanStage,
            storedClient.NextBillingAt,
            storedClient.CreatedAt,
            storedClient.UpdatedAt);
    }

    private sealed record StoredClient(
        Guid Id,
        string Name,
        string EmailCipherText,
        string EmailLookupHash,
        ClientStatus Status,
        Domain.Plans.PlanStage PlanStage,
        DateTimeOffset? NextBillingAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
