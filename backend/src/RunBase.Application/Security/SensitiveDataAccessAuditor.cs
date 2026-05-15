namespace RunBase.Application.Security;

public sealed class SensitiveDataAccessAuditor : ISensitiveDataAccessAuditor
{
    private readonly ISensitiveDataAuditRepository _auditRepository;

    public SensitiveDataAccessAuditor(ISensitiveDataAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<SensitiveDataAccessDecision> RecordAttemptAsync(
        SensitiveDataAccessAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        var previousAttempts = await _auditRepository.CountAttemptsAsync(
            attempt.UserId,
            attempt.ResourceType,
            attempt.ResourceId,
            attempt.SensitiveField,
            cancellationToken);

        var attemptCount = previousAttempts + 1;
        var outcome = attemptCount == 1
            ? SensitiveDataAuditOutcome.Denied
            : SensitiveDataAuditOutcome.Blocked;

        await _auditRepository.SaveAsync(
            new SensitiveDataAuditEntry(
                Guid.NewGuid(),
                attempt.UserId,
                attempt.UserEmail,
                attempt.ResourceType,
                attempt.ResourceId,
                attempt.SensitiveField,
                outcome,
                attemptCount,
                DateTimeOffset.UtcNow),
            cancellationToken);

        return new SensitiveDataAccessDecision(outcome, attemptCount);
    }
}
