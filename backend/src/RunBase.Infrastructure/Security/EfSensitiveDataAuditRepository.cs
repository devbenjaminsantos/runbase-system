using Microsoft.EntityFrameworkCore;
using RunBase.Application.Security;
using RunBase.Infrastructure.Persistence;

namespace RunBase.Infrastructure.Security;

public sealed class EfSensitiveDataAuditRepository : ISensitiveDataAuditRepository
{
    private readonly RunBaseDbContext _dbContext;

    public EfSensitiveDataAuditRepository(RunBaseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CountAttemptsAsync(
        Guid? userId,
        string resourceType,
        Guid resourceId,
        string sensitiveField,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SensitiveDataAuditEntries.CountAsync(
            entry =>
                entry.UserId == userId &&
                entry.ResourceType == resourceType &&
                entry.ResourceId == resourceId &&
                entry.SensitiveField == sensitiveField,
            cancellationToken);
    }

    public async Task SaveAsync(
        SensitiveDataAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SensitiveDataAuditEntries.AddAsync(entry, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
