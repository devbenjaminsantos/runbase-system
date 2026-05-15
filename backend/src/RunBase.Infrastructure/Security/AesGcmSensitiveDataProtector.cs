using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RunBase.Application.Security;

namespace RunBase.Infrastructure.Security;

public sealed class AesGcmSensitiveDataProtector : ISensitiveDataProtector
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const string Prefix = "aesgcm-v1";
    private static readonly byte[] EphemeralDevelopmentKey = RandomNumberGenerator.GetBytes(KeySize);

    private readonly byte[] _key;

    public AesGcmSensitiveDataProtector(IOptions<SensitiveDataProtectionOptions> options)
    {
        _key = GetKey(options.Value);
    }

    public ProtectedSensitiveData Protect(string value)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintext = Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return new ProtectedSensitiveData(
            string.Join(
                '.',
                Prefix,
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(ciphertext),
                Convert.ToBase64String(tag)),
            CreateLookupHash(value));
    }

    public string Unprotect(string cipherText)
    {
        var parts = cipherText.Split('.', 4);

        if (parts.Length != 4 || parts[0] != Prefix)
        {
            throw new InvalidOperationException("Sensitive data payload has an invalid format.");
        }

        var nonce = Convert.FromBase64String(parts[1]);
        var ciphertext = Convert.FromBase64String(parts[2]);
        var tag = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public string CreateLookupHash(string value)
    {
        using var hmac = new HMACSHA256(_key);
        var normalizedValue = Normalize(value);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizedValue));

        return Convert.ToBase64String(hash);
    }

    private static byte[] GetKey(SensitiveDataProtectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Key))
        {
            return EphemeralDevelopmentKey;
        }

        var configuredKey = Convert.FromBase64String(options.Key);

        if (configuredKey.Length != KeySize)
        {
            throw new InvalidOperationException("Sensitive data key must be a 256-bit base64 value.");
        }

        return configuredKey;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
