namespace RunBase.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    public string Issuer { get; init; } = "RunBase";

    public string Audience { get; init; } = "RunBase.Admin";

    public string SigningKey { get; init; } = "runbase-development-signing-key-change-before-production";

    public int AccessTokenMinutes { get; init; } = 60;
}
