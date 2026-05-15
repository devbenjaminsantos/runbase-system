using System.ComponentModel.DataAnnotations;
using RunBase.Domain.Users;

namespace RunBase.Application.Users;

public sealed record UpdateUserRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 2)]
    string Name,
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(254)]
    string Email,
    UserRole Role,
    UserStatus Status);
