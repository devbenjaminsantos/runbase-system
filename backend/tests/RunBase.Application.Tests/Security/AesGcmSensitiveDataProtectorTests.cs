using Microsoft.Extensions.Options;
using RunBase.Infrastructure.Security;

namespace RunBase.Application.Tests.Security;

public sealed class AesGcmSensitiveDataProtectorTests
{
    [Fact]
    public void Protect_DoesNotReturnPlainText()
    {
        var protector = CreateProtector();

        var result = protector.Protect("client@runbase.local");

        Assert.DoesNotContain("client@runbase.local", result.CipherText, StringComparison.Ordinal);
        Assert.NotEmpty(result.LookupHash);
    }

    [Fact]
    public void Unprotect_WithProtectedValue_ReturnsOriginalValue()
    {
        var protector = CreateProtector();
        var protectedValue = protector.Protect("client@runbase.local");

        var result = protector.Unprotect(protectedValue.CipherText);

        Assert.Equal("client@runbase.local", result);
    }

    [Fact]
    public void CreateLookupHash_NormalizesValue()
    {
        var protector = CreateProtector();

        var first = protector.CreateLookupHash("client@runbase.local");
        var second = protector.CreateLookupHash(" CLIENT@RUNBASE.LOCAL ");

        Assert.Equal(first, second);
    }

    private static AesGcmSensitiveDataProtector CreateProtector()
    {
        return new AesGcmSensitiveDataProtector(
            Options.Create(new SensitiveDataProtectionOptions
            {
                Key = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY="
            }));
    }
}
