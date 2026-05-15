namespace RunBase.Application.Users;

public sealed class UserResult<T>
{
    private UserResult(T? value, UserError error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public UserError Error { get; }

    public bool Succeeded => Error == UserError.None;

    public static UserResult<T> Success(T value)
    {
        return new UserResult<T>(value, UserError.None);
    }

    public static UserResult<T> Failure(UserError error)
    {
        return new UserResult<T>(default, error);
    }
}
