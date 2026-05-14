using RunBase.Application.Auth;

namespace RunBase.Infrastructure.Auth;

public sealed class PlainTextPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return password;
    }

    public bool Verify(string password, string passwordHash)
    {
        return string.Equals(password, passwordHash, StringComparison.Ordinal);
    }
}
