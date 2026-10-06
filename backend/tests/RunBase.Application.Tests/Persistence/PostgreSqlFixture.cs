using Microsoft.EntityFrameworkCore;
using RunBase.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RunBase.Application.Tests.Persistence;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17.6-alpine")
        .WithDatabase("runbase_tests")
        .WithUsername("runbase_tests")
        .WithPassword("runbase-tests-only")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public RunBaseDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RunBaseDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new RunBaseDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
