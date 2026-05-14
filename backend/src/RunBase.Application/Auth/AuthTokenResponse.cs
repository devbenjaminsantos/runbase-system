namespace RunBase.Application.Auth;

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserProfileResponse User);
