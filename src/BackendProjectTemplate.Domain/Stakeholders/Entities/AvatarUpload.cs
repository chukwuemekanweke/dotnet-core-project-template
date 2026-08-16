using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.Stakeholders.Entities;

public sealed class AvatarUpload : Entity, IAggregateRoot
{
    private AvatarUpload()
    {
    }

    private AvatarUpload(
        Guid id,
        Guid stakeholderId,
        Guid tenantId,
        string originalFileName,
        string expectedContentType,
        long expectedContentLength,
        string fileExtension,
        string quarantineObjectKey,
        string finalObjectKey,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        StakeholderId = stakeholderId;
        TenantId = tenantId;
        OriginalFileName = originalFileName;
        ExpectedContentType = expectedContentType;
        ExpectedContentLength = expectedContentLength;
        FileExtension = fileExtension;
        QuarantineObjectKey = quarantineObjectKey;
        FinalObjectKey = finalObjectKey;
        ExpiresAtUtc = expiresAtUtc;
        Status = AvatarUploadStatus.Pending;
    }

    public Guid StakeholderId { get; private set; }
    public Guid TenantId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ExpectedContentType { get; private set; } = string.Empty;
    public long ExpectedContentLength { get; private set; }
    public string FileExtension { get; private set; } = string.Empty;
    public string QuarantineObjectKey { get; private set; } = string.Empty;
    public string FinalObjectKey { get; private set; } = string.Empty;
    public string? FinalUrl { get; private set; }
    public string? ValidatedETag { get; private set; }
    public AvatarUploadStatus Status { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public Stakeholder Stakeholder { get; private set; } = null!;

    public static AvatarUpload Create(
        Guid id,
        Guid stakeholderId,
        Guid tenantId,
        string originalFileName,
        string expectedContentType,
        long expectedContentLength,
        string fileExtension,
        string quarantineObjectKey,
        string finalObjectKey,
        DateTimeOffset expiresAtUtc) =>
        new(
            id,
            stakeholderId,
            tenantId,
            originalFileName,
            expectedContentType,
            expectedContentLength,
            fileExtension,
            quarantineObjectKey,
            finalObjectKey,
            expiresAtUtc);

    public void MarkCompleted(string finalUrl, string validatedETag)
    {
        EnsurePending();
        FinalUrl = RequireValue(finalUrl, nameof(finalUrl));
        ValidatedETag = RequireValue(validatedETag, nameof(validatedETag));
        RejectionReason = null;
        Status = AvatarUploadStatus.Completed;
    }

    public void Reject(string reason)
    {
        EnsurePending();
        RejectionReason = RequireValue(reason, nameof(reason));
        Status = AvatarUploadStatus.Rejected;
    }

    public void MarkExpired()
    {
        EnsurePending();
        Status = AvatarUploadStatus.Expired;
    }

    private void EnsurePending()
    {
        if (Status != AvatarUploadStatus.Pending)
        {
            throw new AggregateStateException($"Avatar upload in '{Status}' state cannot be changed.");
        }
    }

    private static string RequireValue(string value, string argumentName)
    {
        var normalized = value.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException("A value is required.", argumentName)
            : normalized;
    }
}
