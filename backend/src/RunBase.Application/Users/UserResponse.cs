using RunBase.Domain.Users;

namespace RunBase.Application.Users;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
