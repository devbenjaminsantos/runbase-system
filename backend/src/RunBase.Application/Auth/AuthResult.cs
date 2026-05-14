namespace RunBase.Application.Auth;

public sealed record AuthResult<T>(
    bool Succeeded,
    T? Value,
    AuthError? Error)
{
    public static AuthResult<T> Success(T value)
    {
        return new AuthResult<T>(true, value, null);
    }

    public static AuthResult<T> Failure(AuthError error)
    {
        return new AuthResult<T>(false, default, error);
    }
}
