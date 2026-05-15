using System.ComponentModel.DataAnnotations;

namespace RunBase.Application.Auth;

public sealed record LoginRequest(
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(254)]
    string Email,
    [property: Required]
    [property: MinLength(1)]
    [property: StringLength(128)]
    string Password);
