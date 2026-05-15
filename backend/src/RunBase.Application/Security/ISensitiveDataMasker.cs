namespace RunBase.Application.Security;

public interface ISensitiveDataMasker
{
    string MaskEmail(string email);

    string MaskPhone(string phone);

    string MaskDocument(string document);
}
