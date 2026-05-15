namespace RunBase.Application.Security;

public interface ISensitiveDataAuditRepository
{
    Task<int> CountAttemptsAsync(
        Guid? userId,
        string resourceType,
        Guid resourceId,
        string sensitiveField,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        SensitiveDataAuditEntry entry,
        CancellationToken cancellationToken = default);
}
