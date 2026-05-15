namespace RunBase.Application.Users;

public interface IUsersService
{
    Task<IReadOnlyList<UserResponse>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<UserResult<UserResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UserResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserResult<UserResponse>> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserResult<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
