using RunBase.Domain.Users;

namespace RunBase.Application.Users;

public sealed record UpdateUserRequest(
    string Name,
    string Email,
    UserRole Role,
    UserStatus Status);
