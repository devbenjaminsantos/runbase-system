namespace RunBase.Application.Demo;

public interface IDemoDataGenerator
{
    IReadOnlyList<DemoClientResponse> GenerateClients(
        int count,
        DateTimeOffset referenceDateUtc);
}
