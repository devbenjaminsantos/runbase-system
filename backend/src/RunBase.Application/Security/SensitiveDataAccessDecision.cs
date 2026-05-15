namespace RunBase.Application.Security;

public sealed record SensitiveDataAccessDecision(
    SensitiveDataAuditOutcome Outcome,
    int AttemptCount);
