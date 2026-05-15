using RunBase.Application.Security;

namespace RunBase.Application.Tests.Security;

public sealed class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskEmail_MasksLocalPart()
    {
        var masker = new SensitiveDataMasker();

        var result = masker.MaskEmail("client@example.com");

        Assert.Equal("cl***@example.com", result);
    }

    [Fact]
    public void MaskPhone_LeavesOnlyLastFourDigits()
    {
        var masker = new SensitiveDataMasker();

        var result = masker.MaskPhone("+55 11 99999-1234");

        Assert.Equal("***-***-1234", result);
    }

    [Fact]
    public void MaskDocument_LeavesOnlyLastTwoDigits()
    {
        var masker = new SensitiveDataMasker();

        var result = masker.MaskDocument("123.456.789-09");

        Assert.Equal("***.***.***-09", result);
    }
}
