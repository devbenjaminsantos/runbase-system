namespace RunBase.Application.Security;

public interface ISensitiveDataAccessAuditor
{
    Task<SensitiveDataAccessDecision> RecordAttemptAsync(
        SensitiveDataAccessAttempt attempt,
        CancellationToken cancellationToken = default);
}
