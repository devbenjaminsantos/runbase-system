namespace RunBase.Application.Tests.Security;

public sealed class SqlInjectionGuardTests
{
    [Fact]
    public void SourceCode_DoesNotUseRawSqlApis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "backend", "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        var forbiddenUsages = sourceFiles
            .Select(path => new
            {
                Path = path,
                Content = File.ReadAllText(path)
            })
            .Where(file =>
                file.Content.Contains("FromSqlRaw", StringComparison.Ordinal) ||
                file.Content.Contains("ExecuteSqlRaw", StringComparison.Ordinal) ||
                file.Content.Contains("SqlQueryRaw", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repositoryRoot, file.Path))
            .ToList();

        Assert.Empty(forbiddenUsages);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ??
            throw new InvalidOperationException("Could not find repository root.");
    }
}
