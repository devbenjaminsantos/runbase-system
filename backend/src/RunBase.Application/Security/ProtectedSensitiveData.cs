namespace RunBase.Application.Security;

public sealed record ProtectedSensitiveData(
    string CipherText,
    string LookupHash);
