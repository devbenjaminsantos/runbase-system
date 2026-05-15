using RunBase.Application.Security;

namespace RunBase.Application.Tests.Security;

public sealed class SensitiveLogSanitizerTests
{
    [Theory]
    [InlineData("password", "Admin123!")]
    [InlineData("email", "admin@runbase.local")]
    [InlineData("phone", "+55 11 99999-1234")]
    [InlineData("document", "123.456.789-09")]
    [InlineData("refreshToken", "secret-refresh-token")]
    [InlineData("Authorization", "Bearer secret-token")]
    public void Sanitize_WithSensitiveField_RedactsValue(
        string fieldName,
        string value)
    {
        var sanitizer = new SensitiveLogSanitizer();

        var result = sanitizer.Sanitize(fieldName, value);

        Assert.Equal("[REDACTED]", result);
    }

    [Fact]
    public void Sanitize_WithNonSensitiveField_KeepsValue()
    {
        var sanitizer = new SensitiveLogSanitizer();

        var result = sanitizer.Sanitize("status", "Active");

        Assert.Equal("Active", result);
    }

    [Fact]
    public void Sanitize_WithDictionary_RedactsOnlySensitiveValues()
    {
        var sanitizer = new SensitiveLogSanitizer();

        var result = sanitizer.Sanitize(new Dictionary<string, string?>
        {
            ["email"] = "client@runbase.local",
            ["status"] = "Active"
        });

        Assert.Equal("[REDACTED]", result["email"]);
        Assert.Equal("Active", result["status"]);
    }
}
