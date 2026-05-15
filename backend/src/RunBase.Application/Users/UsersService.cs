using RunBase.Application.Auth;
using RunBase.Domain.Users;

namespace RunBase.Application.Users;

public sealed class UsersService : IUsersService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public UsersService(
        IUserRepository users,
        IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _users.ListAsync(cancellationToken);

        return users
            .OrderBy(user => user.Name)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<UserResult<UserResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);

        return user is null
            ? UserResult<UserResponse>.Failure(UserError.NotFound)
            : UserResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<UserResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await _users.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            return UserResult<UserResponse>.Failure(UserError.EmailAlreadyExists);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User(
            Guid.NewGuid(),
            request.Name,
            request.Email,
            _passwordHasher.Hash(request.Password),
            request.Role,
            request.Status,
            now,
            now);

        await _users.SaveAsync(user, cancellationToken);

        return UserResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<UserResult<UserResponse>> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return UserResult<UserResponse>.Failure(UserError.NotFound);
        }

        if (await _users.EmailExistsAsync(request.Email, id, cancellationToken))
        {
            return UserResult<UserResponse>.Failure(UserError.EmailAlreadyExists);
        }

        if (user.Role == UserRole.Admin &&
            user.Status == UserStatus.Active &&
            (request.Role != UserRole.Admin || request.Status != UserStatus.Active) &&
            !await HasAnotherActiveAdminAsync(user.Id, cancellationToken))
        {
            return UserResult<UserResponse>.Failure(UserError.LastActiveAdminRequired);
        }

        user.Update(
            request.Name,
            request.Email,
            request.Role,
            request.Status,
            DateTimeOffset.UtcNow);

        await _users.SaveAsync(user, cancellationToken);

        return UserResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<UserResult<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return UserResult<bool>.Failure(UserError.NotFound);
        }

        if (user.Role == UserRole.Admin &&
            user.Status == UserStatus.Active &&
            !await HasAnotherActiveAdminAsync(user.Id, cancellationToken))
        {
            return UserResult<bool>.Failure(UserError.LastActiveAdminRequired);
        }

        await _users.DeleteAsync(user, cancellationToken);

        return UserResult<bool>.Success(true);
    }

    private async Task<bool> HasAnotherActiveAdminAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var users = await _users.ListAsync(cancellationToken);

        return users.Any(user =>
            user.Id != userId &&
            user.Role == UserRole.Admin &&
            user.Status == UserStatus.Active);
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.Status,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
