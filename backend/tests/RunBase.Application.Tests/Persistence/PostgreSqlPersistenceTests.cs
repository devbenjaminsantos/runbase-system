using Microsoft.EntityFrameworkCore;
using Npgsql;
using RunBase.Domain.Organizations;
using RunBase.Domain.Plans;

namespace RunBase.Application.Tests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlPersistenceTests
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlPersistenceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrations_CreateExpectedSchemaAndDatabaseConstraints()
    {
        await using var context = _fixture.CreateContext();
        var availableMigrations = context.Database.GetMigrations().ToArray();
        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();

        Assert.NotEmpty(availableMigrations);
        Assert.Equal(availableMigrations, appliedMigrations);

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT numeric_precision, numeric_scale
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'plans'
              AND column_name = 'Price';
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(18, reader.GetInt32(0));
        Assert.Equal(2, reader.GetInt32(1));
        await reader.CloseAsync();

        var now = DateTimeOffset.UtcNow;
        var slug = $"postgres-{Guid.NewGuid():N}";
        context.Organizations.Add(new Organization(
            Guid.NewGuid(),
            "PostgreSQL verification",
            slug,
            OrganizationStatus.Active,
            now,
            now));
        context.Plans.Add(new Plan(
            Guid.NewGuid(),
            "PostgreSQL Plus",
            PlanStage.Plus,
            123.45m,
            BillingCycle.Monthly,
            true,
            now.AddMonths(1),
            now,
            now));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(123.45m, await context.Plans.Select(plan => plan.Price).SingleAsync());

        context.Organizations.Add(new Organization(
            Guid.NewGuid(),
            "Duplicate slug",
            slug,
            OrganizationStatus.Active,
            now,
            now));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
    }

    [Fact]
    public async Task TransactionRollback_DoesNotPersistChanges()
    {
        var organizationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var context = _fixture.CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Organizations.Add(new Organization(
                organizationId,
                "Rolled back organization",
                $"rollback-{organizationId:N}",
                OrganizationStatus.Active,
                now,
                now));
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verificationContext = _fixture.CreateContext();
        Assert.False(await verificationContext.Organizations.AnyAsync(
            organization => organization.Id == organizationId));
    }
}
