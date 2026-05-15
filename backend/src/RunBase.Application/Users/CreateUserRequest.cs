using RunBase.Domain.Users;

namespace RunBase.Application.Users;

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    UserRole Role,
    UserStatus Status);
