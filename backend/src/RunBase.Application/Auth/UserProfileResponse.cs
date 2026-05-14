using RunBase.Domain.Users;

namespace RunBase.Application.Auth;

public sealed record UserProfileResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    UserStatus Status);
