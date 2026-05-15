namespace RunBase.Application.Security;

public sealed record SensitiveDataAuditEntry(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string ResourceType,
    Guid ResourceId,
    string SensitiveField,
    SensitiveDataAuditOutcome Outcome,
    int AttemptCount,
    DateTimeOffset CreatedAtUtc);
