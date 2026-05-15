using System.Collections.Concurrent;
using RunBase.Application.Security;

namespace RunBase.Infrastructure.Security;

public sealed class InMemorySensitiveDataAuditRepository : ISensitiveDataAuditRepository
{
    private readonly ConcurrentBag<SensitiveDataAuditEntry> _entries = new();

    public Task<int> CountAttemptsAsync(
        Guid? userId,
        string resourceType,
        Guid resourceId,
        string sensitiveField,
        CancellationToken cancellationToken = default)
    {
        var count = _entries.Count(entry =>
            entry.UserId == userId &&
            entry.ResourceType == resourceType &&
            entry.ResourceId == resourceId &&
            entry.SensitiveField == sensitiveField);

        return Task.FromResult(count);
    }

    public Task SaveAsync(
        SensitiveDataAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);

        return Task.CompletedTask;
    }
}
