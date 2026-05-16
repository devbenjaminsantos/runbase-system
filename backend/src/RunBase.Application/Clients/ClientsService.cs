using RunBase.Domain.Clients;
using RunBase.Domain.Plans;
using RunBase.Application.Security;
using RunBase.Domain;

namespace RunBase.Application.Clients;

public sealed class ClientsService : IClientsService
{
    private readonly IClientRepository _clients;
    private readonly ISensitiveDataMasker _sensitiveDataMasker;

    public ClientsService(
        IClientRepository clients,
        ISensitiveDataMasker sensitiveDataMasker)
    {
        _clients = clients;
        _sensitiveDataMasker = sensitiveDataMasker;
    }

    public async Task<IReadOnlyList<ClientResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var clients = await _clients.ListAsync(cancellationToken);

        return clients
            .OrderBy(client => client.Name)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<ClientResult<ClientResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAsync(id, cancellationToken);

        return client is null
            ? ClientResult<ClientResponse>.Failure(ClientError.NotFound)
            : ClientResult<ClientResponse>.Success(ToResponse(client));
    }

    public async Task<ClientResult<ClientResponse>> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (Plan.RequiresBillingDate(request.PlanStage) && request.NextBillingAt is null)
        {
            return ClientResult<ClientResponse>.Failure(ClientError.BillingDateRequired);
        }

        if (await _clients.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            return ClientResult<ClientResponse>.Failure(ClientError.EmailAlreadyExists);
        }

        var now = DateTimeOffset.UtcNow;
        var client = new Client(
            Guid.NewGuid(),
            request.Name,
            request.Email,
            request.Status,
            request.PlanStage,
            request.DataSource ?? DataSource.Manual,
            request.NextBillingAt,
            now,
            now);

        await _clients.SaveAsync(client, cancellationToken);

        return ClientResult<ClientResponse>.Success(ToResponse(client));
    }

    public async Task<ClientResult<ClientResponse>> UpdateAsync(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAsync(id, cancellationToken);

        if (client is null)
        {
            return ClientResult<ClientResponse>.Failure(ClientError.NotFound);
        }

        if (Plan.RequiresBillingDate(request.PlanStage) && request.NextBillingAt is null)
        {
            return ClientResult<ClientResponse>.Failure(ClientError.BillingDateRequired);
        }

        if (await _clients.EmailExistsAsync(request.Email, id, cancellationToken))
        {
            return ClientResult<ClientResponse>.Failure(ClientError.EmailAlreadyExists);
        }

        client.Update(
            request.Name,
            request.Email,
            request.Status,
            request.PlanStage,
            request.DataSource,
            request.NextBillingAt,
            DateTimeOffset.UtcNow);

        await _clients.SaveAsync(client, cancellationToken);

        return ClientResult<ClientResponse>.Success(ToResponse(client));
    }

    public async Task<ClientResult<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAsync(id, cancellationToken);

        if (client is null)
        {
            return ClientResult<bool>.Failure(ClientError.NotFound);
        }

        await _clients.DeleteAsync(client, cancellationToken);

        return ClientResult<bool>.Success(true);
    }

    private ClientResponse ToResponse(Client client)
    {
        return new ClientResponse(
            client.Id,
            client.Name,
            _sensitiveDataMasker.MaskEmail(client.Email),
            client.Status,
            client.PlanStage,
            client.DataSource,
            client.NextBillingAt,
            client.CreatedAt,
            client.UpdatedAt);
    }
}
