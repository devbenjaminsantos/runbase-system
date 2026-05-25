using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RunBase.Infrastructure.Persistence;

public sealed class RunBaseDbContextFactory : IDesignTimeDbContextFactory<RunBaseDbContext>
{
    private const string DefaultConnectionStringEnvironmentKey = "ConnectionStrings__DefaultConnection";
    private const string LocalWindowsConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=RunBase;Trusted_Connection=True;TrustServerCertificate=True;";

    public RunBaseDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(DefaultConnectionStringEnvironmentKey);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = LocalWindowsConnectionString;
        }

        var optionsBuilder = new DbContextOptionsBuilder<RunBaseDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions => sqlOptions.EnableRetryOnFailure());

        return new RunBaseDbContext(optionsBuilder.Options);
    }
}
