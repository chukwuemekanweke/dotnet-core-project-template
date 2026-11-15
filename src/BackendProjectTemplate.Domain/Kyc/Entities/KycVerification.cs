using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.Kyc.Entities;

public sealed class KycVerification : Entity, IAggregateRoot
{
    private KycVerification()
    {
    }

    private KycVerification(Guid tenantId, Guid stakeholderId, string providerKey, string externalApplicantId, string? applicantReference, DateTimeOffset startedAtUtc)
    {
        TenantId = tenantId;
        StakeholderId = stakeholderId;
        ProviderKey = providerKey;
        ExternalApplicantId = externalApplicantId;
        ProviderApplicantReference = NormalizeOptional(applicantReference);
        Status = KycVerificationStatus.Pending;
        ProviderStatus = "unused";
        StartedAtUtc = startedAtUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid StakeholderId { get; private set; }
    public string ProviderKey { get; private set; } = string.Empty;
    public string ExternalApplicantId { get; private set; } = string.Empty;
    public string? ProviderApplicantReference { get; private set; }
    public string? ProviderVerificationReference { get; private set; }
    public string? ProviderSessionReference { get; private set; }
    public string ProviderStatus { get; private set; } = string.Empty;
    public KycVerificationStatus Status { get; private set; }
    public string? VerificationUrl { get; private set; }
    public DateTimeOffset? FormUrlExpiresAtUtc { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public bool IsActive => Status is KycVerificationStatus.Pending or KycVerificationStatus.InProgress;

    public static KycVerification Start(Guid tenantId, Guid stakeholderId, string providerKey, string externalApplicantId, string? applicantReference, DateTimeOffset startedAtUtc) =>
        new(tenantId, stakeholderId, providerKey.Trim(), externalApplicantId.Trim(), applicantReference, startedAtUtc);

    public void AttachSession(KycSessionResult session)
    {
        EnsureActive();
        ProviderVerificationReference = NormalizeOptional(session.VerificationReference);
        ProviderSessionReference = session.SessionReference.Trim();
        VerificationUrl = session.VerificationUrl.AbsoluteUri;
    }

    public void ApplyProviderResult(string providerStatus, bool? verified, string? applicantReference, DateTimeOffset utcNow)
    {
        if (Status is KycVerificationStatus.Verified or KycVerificationStatus.Rejected)
        {
            return;
        }

        ProviderStatus = providerStatus.Trim().ToLowerInvariant();
        ProviderApplicantReference ??= NormalizeOptional(applicantReference);

        if (ProviderStatus == "completed" && verified.HasValue)
        {
            Status = verified.Value ? KycVerificationStatus.Verified : KycVerificationStatus.Rejected;
            CompletedAtUtc = utcNow;
            VerificationUrl = null;
            return;
        }

        if (ProviderStatus == "pending")
        {
            Status = KycVerificationStatus.InProgress;
            SubmittedAtUtc ??= utcNow;
            VerificationUrl = null;
        }
    }

    public void MarkExpired(DateTimeOffset utcNow)
    {
        EnsureActive();
        Status = KycVerificationStatus.Expired;
        ProviderStatus = "expired";
        FormUrlExpiresAtUtc = utcNow;
        CompletedAtUtc = utcNow;
        VerificationUrl = null;
    }

    public void MarkFailed(DateTimeOffset utcNow)
    {
        EnsureActive();
        Status = KycVerificationStatus.Failed;
        ProviderStatus = "provider_error";
        CompletedAtUtc = utcNow;
        VerificationUrl = null;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new AggregateStateException($"KYC verification '{Id}' cannot transition from '{Status}'.");
        }
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
