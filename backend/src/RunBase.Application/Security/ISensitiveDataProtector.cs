namespace RunBase.Application.Security;

public interface ISensitiveDataProtector
{
    ProtectedSensitiveData Protect(string value);

    string Unprotect(string cipherText);

    string CreateLookupHash(string value);
}
