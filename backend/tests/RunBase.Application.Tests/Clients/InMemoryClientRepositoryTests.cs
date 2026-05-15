using Microsoft.Extensions.Options;
using RunBase.Domain.Clients;
using RunBase.Domain.Plans;
using RunBase.Infrastructure.Clients;
using RunBase.Infrastructure.Security;

namespace RunBase.Application.Tests.Clients;

public sealed class InMemoryClientRepositoryTests
{
    [Fact]
    public async Task SaveAsync_StoresProtectedEmailButReturnsDomainClient()
    {
        var repository = CreateRepository();
        var client = CreateClient("client@runbase.local");

        await repository.SaveAsync(client);

        var result = await repository.GetByIdAsync(client.Id);

        Assert.NotNull(result);
        Assert.Equal("client@runbase.local", result.Email);
    }

    [Fact]
    public async Task EmailExistsAsync_UsesNormalizedLookupHash()
    {
        var repository = CreateRepository();

        await repository.SaveAsync(CreateClient("client@runbase.local"));

        var exists = await repository.EmailExistsAsync(" CLIENT@RUNBASE.LOCAL ");

        Assert.True(exists);
    }

    private static InMemoryClientRepository CreateRepository()
    {
        var protector = new AesGcmSensitiveDataProtector(
            Options.Create(new SensitiveDataProtectionOptions
            {
                Key = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY="
            }));

        return new InMemoryClientRepository(protector);
    }

    private static Client CreateClient(string email)
    {
        var now = DateTimeOffset.UtcNow;

        return new Client(
            Guid.NewGuid(),
            "Protected Client",
            email,
            ClientStatus.Active,
            PlanStage.Free,
            null,
            now,
            now);
    }
}
