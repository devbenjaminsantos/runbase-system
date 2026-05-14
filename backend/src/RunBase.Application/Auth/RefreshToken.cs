namespace RunBase.Application.Auth;

public sealed class RefreshToken
{
    public RefreshToken(
        string value,
        Guid userId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? revokedAtUtc = null)
    {
        Value = value;
        UserId = userId;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        RevokedAtUtc = revokedAtUtc;
    }

    public string Value { get; }

    public Guid UserId { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAtUtc is null && ExpiresAtUtc > now;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        RevokedAtUtc ??= revokedAtUtc;
    }
}
