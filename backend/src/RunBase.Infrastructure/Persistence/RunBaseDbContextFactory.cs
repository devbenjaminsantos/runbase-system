using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RunBase.Infrastructure.Persistence;

public sealed class RunBaseDbContextFactory : IDesignTimeDbContextFactory<RunBaseDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=RunBase;Trusted_Connection=True;TrustServerCertificate=True;";

    public RunBaseDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RunBaseDbContext>();
        optionsBuilder.UseSqlServer(DesignTimeConnectionString);

        return new RunBaseDbContext(optionsBuilder.Options);
    }
}
