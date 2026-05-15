namespace RunBase.Application.Clients;

public sealed class ClientResult<T>
{
    private ClientResult(T? value, ClientError error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public ClientError Error { get; }

    public bool Succeeded => Error == ClientError.None;

    public static ClientResult<T> Success(T value)
    {
        return new ClientResult<T>(value, ClientError.None);
    }

    public static ClientResult<T> Failure(ClientError error)
    {
        return new ClientResult<T>(default, error);
    }
}
