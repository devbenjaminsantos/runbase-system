using RunBase.Application.Security;

namespace RunBase.Application.Tests.Security;

public sealed class SensitiveDataAccessAuditorTests
{
    [Fact]
    public async Task RecordAttemptAsync_FirstAttempt_IsDeniedAndAudited()
    {
        var repository = new FakeSensitiveDataAuditRepository();
        var auditor = new SensitiveDataAccessAuditor(repository);
        var attempt = CreateAttempt();

        var decision = await auditor.RecordAttemptAsync(attempt);

        Assert.Equal(SensitiveDataAuditOutcome.Denied, decision.Outcome);
        Assert.Equal(1, decision.AttemptCount);
        Assert.Single(repository.Entries);
        Assert.Equal(SensitiveDataAuditOutcome.Denied, repository.Entries[0].Outcome);
    }

    [Fact]
    public async Task RecordAttemptAsync_RepeatedAttempt_IsBlockedAndAudited()
    {
        var repository = new FakeSensitiveDataAuditRepository();
        var auditor = new SensitiveDataAccessAuditor(repository);
        var attempt = CreateAttempt();

        await auditor.RecordAttemptAsync(attempt);
        var decision = await auditor.RecordAttemptAsync(attempt);

        Assert.Equal(SensitiveDataAuditOutcome.Blocked, decision.Outcome);
        Assert.Equal(2, decision.AttemptCount);
        Assert.Equal(2, repository.Entries.Count);
        Assert.Equal(SensitiveDataAuditOutcome.Blocked, repository.Entries[1].Outcome);
    }

    private static SensitiveDataAccessAttempt CreateAttempt()
    {
        return new SensitiveDataAccessAttempt(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "admin@runbase.local",
            "Client",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Email");
    }

    private sealed class FakeSensitiveDataAuditRepository : ISensitiveDataAuditRepository
    {
        public List<SensitiveDataAuditEntry> Entries { get; } = [];

        public Task<int> CountAttemptsAsync(
            Guid? userId,
            string resourceType,
            Guid resourceId,
            string sensitiveField,
            CancellationToken cancellationToken = default)
        {
            var count = Entries.Count(entry =>
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
            Entries.Add(entry);

            return Task.CompletedTask;
        }
    }
}
