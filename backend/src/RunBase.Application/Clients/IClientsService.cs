namespace RunBase.Application.Clients;

public interface IClientsService
{
    Task<IReadOnlyList<ClientResponse>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<ClientResult<ClientResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ClientResult<ClientResponse>> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientResult<ClientResponse>> UpdateAsync(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientResult<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
