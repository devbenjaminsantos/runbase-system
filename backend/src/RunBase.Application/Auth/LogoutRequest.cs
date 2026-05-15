using System.ComponentModel.DataAnnotations;

namespace RunBase.Application.Auth;

public sealed record LogoutRequest(
    [property: Required]
    [property: MinLength(1)]
    string RefreshToken);
