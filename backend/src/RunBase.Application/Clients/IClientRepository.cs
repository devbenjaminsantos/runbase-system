using RunBase.Domain.Clients;

namespace RunBase.Application.Clients;

public interface IClientRepository
{
    Task<IReadOnlyList<Client>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        Guid? exceptClientId = null,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Client client,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Client client,
        CancellationToken cancellationToken = default);
}
