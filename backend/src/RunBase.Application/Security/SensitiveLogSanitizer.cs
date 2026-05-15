namespace RunBase.Application.Security;

public sealed class SensitiveLogSanitizer : ISensitiveLogSanitizer
{
    private static readonly string[] SensitiveFieldNames =
    [
        "password",
        "token",
        "refreshToken",
        "authorization",
        "email",
        "phone",
        "document",
        "cpf",
        "cnpj"
    ];

    public string Sanitize(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return IsSensitive(fieldName)
            ? "[REDACTED]"
            : value;
    }

    public IReadOnlyDictionary<string, string> Sanitize(
        IReadOnlyDictionary<string, string?> fields)
    {
        return fields.ToDictionary(
            field => field.Key,
            field => Sanitize(field.Key, field.Value));
    }

    private static bool IsSensitive(string fieldName)
    {
        return SensitiveFieldNames.Any(sensitiveField =>
            string.Equals(fieldName, sensitiveField, StringComparison.OrdinalIgnoreCase));
    }
}
