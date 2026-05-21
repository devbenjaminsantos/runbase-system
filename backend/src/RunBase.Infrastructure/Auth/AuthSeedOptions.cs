namespace RunBase.Infrastructure.Auth;

public sealed class AuthSeedOptions
{
    public const string SectionName = "Auth:SeedAdmin";

    public string Id { get; init; } = "11111111-1111-1111-1111-111111111111";

    public string Name { get; init; } = "RunBase Admin";

    public string Email { get; init; } = "admin@runbase.local";

    public string Password { get; init; } = string.Empty;
}
