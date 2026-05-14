namespace RunBase.Application.Auth;

public sealed record AccessToken(
    string Value,
    DateTimeOffset ExpiresAtUtc);
