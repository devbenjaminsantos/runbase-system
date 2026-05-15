namespace RunBase.Infrastructure.Security;

public sealed class SensitiveDataProtectionOptions
{
    public const string SectionName = "Security:SensitiveData";

    public string? Key { get; init; }
}
