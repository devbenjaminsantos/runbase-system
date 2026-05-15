namespace RunBase.Application.Auth;

public sealed record LogoutRequest(
    string RefreshToken);
