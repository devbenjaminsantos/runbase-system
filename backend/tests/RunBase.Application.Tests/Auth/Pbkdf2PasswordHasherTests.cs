using RunBase.Infrastructure.Auth;

namespace RunBase.Application.Tests.Auth;

public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Hash_ReturnsVersionedHash()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hash = hasher.Hash("Admin123!");

        Assert.StartsWith("pbkdf2-sha256.", hash, StringComparison.Ordinal);
        Assert.NotEqual("Admin123!", hash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("Admin123!");

        var result = hasher.Verify("Admin123!", hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("Admin123!");

        var result = hasher.Verify("Wrong123!", hash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_WithInvalidHash_ReturnsFalse()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var result = hasher.Verify("Admin123!", "not-a-valid-hash");

        Assert.False(result);
    }
}
