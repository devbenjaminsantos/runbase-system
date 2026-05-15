using System.ComponentModel.DataAnnotations;
using RunBase.Domain.Users;

namespace RunBase.Application.Users;

public sealed record CreateUserRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 2)]
    string Name,
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(254)]
    string Email,
    [property: Required]
    [property: MinLength(8)]
    [property: StringLength(128)]
    string Password,
    UserRole Role,
    UserStatus Status);
