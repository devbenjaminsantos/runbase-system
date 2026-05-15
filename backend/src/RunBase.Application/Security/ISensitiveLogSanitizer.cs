namespace RunBase.Application.Security;

public interface ISensitiveLogSanitizer
{
    string Sanitize(string fieldName, string? value);

    IReadOnlyDictionary<string, string> Sanitize(
        IReadOnlyDictionary<string, string?> fields);
}
