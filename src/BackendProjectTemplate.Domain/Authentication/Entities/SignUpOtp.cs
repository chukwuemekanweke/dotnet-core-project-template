using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Authentication.Entities;

public sealed class SignUpOtp : Entity
{
    private SignUpOtp()
    {
    }

    private SignUpOtp(
        Guid userId,
        string normalizedEmail,
        string codeHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset utcNow)
    {
        UserId = userId;
        NormalizedEmail = normalizedEmail;
        CodeHash = codeHash;
        ExpiresAtUtc = expiresAtUtc;
        SetAuditDates(utcNow);
    }

    public Guid UserId { get; private set; }
    public AppUser User { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public static SignUpOtp Create(
        Guid userId,
        string normalizedEmail,
        string codeHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset utcNow) =>
        new(userId, normalizedEmail, codeHash, expiresAtUtc, utcNow);

    public bool IsAvailable(DateTimeOffset utcNow) =>
        ConsumedAtUtc is null && ExpiresAtUtc > utcNow;

    public void MarkConsumed(DateTimeOffset utcNow)
    {
        ConsumedAtUtc = utcNow;
        Touch(utcNow);
    }
}
