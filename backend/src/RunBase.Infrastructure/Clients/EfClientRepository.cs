using Microsoft.EntityFrameworkCore;
using RunBase.Application.Clients;
using RunBase.Application.Security;
using RunBase.Domain.Clients;
using RunBase.Infrastructure.Persistence;

namespace RunBase.Infrastructure.Clients;

public sealed class EfClientRepository : IClientRepository
{
    private readonly RunBaseDbContext _dbContext;
    private readonly ISensitiveDataProtector _sensitiveDataProtector;

    public EfClientRepository(
        RunBaseDbContext dbContext,
        ISensitiveDataProtector sensitiveDataProtector)
    {
        _dbContext = dbContext;
        _sensitiveDataProtector = sensitiveDataProtector;
    }

    public async Task<IReadOnlyList<Client>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Clients
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return records.Select(ToClient).ToList();
    }

    public async Task<Client?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(client => client.Id == id, cancellationToken);

        return record is null ? null : ToClient(record);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? exceptClientId = null,
        CancellationToken cancellationToken = default)
    {
        var lookupHash = _sensitiveDataProtector.CreateLookupHash(email);

        return await _dbContext.Clients.AnyAsync(
            client =>
                (!exceptClientId.HasValue || client.Id != exceptClientId.Value) &&
                client.EmailLookupHash == lookupHash,
            cancellationToken);
    }

    public async Task SaveAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        var protectedEmail = _sensitiveDataProtector.Protect(client.Email);
        var record = new ClientRecord
        {
            Id = client.Id,
            Name = client.Name,
            EmailCipherText = protectedEmail.CipherText,
            EmailLookupHash = protectedEmail.LookupHash,
            Status = client.Status,
            PlanStage = client.PlanStage,
            DataSource = client.DataSource,
            NextBillingAt = client.NextBillingAt,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };

        var exists = await _dbContext.Clients.AnyAsync(
            existingClient => existingClient.Id == client.Id,
            cancellationToken);

        if (exists)
        {
            _dbContext.Clients.Update(record);
        }
        else
        {
            await _dbContext.Clients.AddAsync(record, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Clients.FirstOrDefaultAsync(
            existingClient => existingClient.Id == client.Id,
            cancellationToken);

        if (record is null)
        {
            return;
        }

        _dbContext.Clients.Remove(record);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Client ToClient(ClientRecord record)
    {
        return new Client(
            record.Id,
            record.Name,
            _sensitiveDataProtector.Unprotect(record.EmailCipherText),
            record.Status,
            record.PlanStage,
            record.DataSource,
            record.NextBillingAt,
            record.CreatedAt,
            record.UpdatedAt);
    }
}
