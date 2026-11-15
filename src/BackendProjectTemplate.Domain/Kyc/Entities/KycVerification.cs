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
    public DateTimeOffset? SessionRequestedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? LastReconciledAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public bool IsActive => Status is KycVerificationStatus.Pending or KycVerificationStatus.InProgress;

    /// <summary>
    /// How long an unanswered provider session request blocks a new one. A request whose outcome is unknown
    /// (timeout, transport failure, 5xx) may have created a form at the provider; within this window the
    /// attempt is not re-requested so a slow provider response cannot produce a duplicate form. After it,
    /// the request is treated as abandoned and a new session may be requested.
    /// </summary>
    public static readonly TimeSpan SessionProvisioningLease = TimeSpan.FromMinutes(10);

    public static KycVerification Start(Guid tenantId, Guid stakeholderId, string providerKey, string externalApplicantId, string? applicantReference, DateTimeOffset startedAtUtc) =>
        new(tenantId, stakeholderId, providerKey.Trim(), externalApplicantId.Trim(), applicantReference, startedAtUtc);

    public bool HasPendingSessionRequest(DateTimeOffset utcNow) =>
        ProviderSessionReference is null &&
        SessionRequestedAtUtc.HasValue &&
        SessionRequestedAtUtc.Value + SessionProvisioningLease > utcNow;

    public void MarkSessionRequested(DateTimeOffset utcNow)
    {
        EnsureActive();
        if (ProviderSessionReference is not null)
        {
            throw new AggregateStateException($"KYC verification '{Id}' already has a provider session.");
        }

        if (HasPendingSessionRequest(utcNow))
        {
            throw new AggregateStateException($"KYC verification '{Id}' already has a pending session provisioning request.");
        }

        SessionRequestedAtUtc = utcNow;
    }

    /// <summary>
    /// Clears a session request the provider definitively did not fulfil, so a retry is allowed immediately.
    /// </summary>
    public void ReleaseSessionRequest()
    {
        EnsureActive();
        if (ProviderSessionReference is not null)
        {
            throw new AggregateStateException($"KYC verification '{Id}' already has a provider session.");
        }

        SessionRequestedAtUtc = null;
    }

    public void AttachSession(KycSessionResult session)
    {
        EnsureActive();
        if (!SessionRequestedAtUtc.HasValue)
        {
            throw new AggregateStateException($"KYC verification '{Id}' has no session provisioning request.");
        }

        ProviderVerificationReference = NormalizeOptional(session.VerificationReference);
        ProviderSessionReference = session.SessionReference.Trim();
        VerificationUrl = session.VerificationUrl.AbsoluteUri;
        FormUrlExpiresAtUtc = null;
    }

    public void RecordProviderVerificationReference(string? verificationReference)
    {
        if (IsActive && ProviderVerificationReference is null)
        {
            ProviderVerificationReference = NormalizeOptional(verificationReference);
        }
    }

    public void ApplyProviderResult(string providerStatus, bool? verified, string? applicantReference, DateTimeOffset utcNow)
    {
        if (!IsActive)
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

    public void RecordReconciliation(DateTimeOffset utcNow) => LastReconciledAtUtc = utcNow;

    public void MarkFormUrlExpired(DateTimeOffset utcNow)
    {
        EnsureActive();
        VerificationUrl = null;
        FormUrlExpiresAtUtc = utcNow;
        if (Status != KycVerificationStatus.Pending)
        {
            // The applicant already submitted; the provider decision is still outstanding and must stay
            // correlatable by its verification and session references.
            return;
        }

        ProviderVerificationReference = null;
        ProviderSessionReference = null;
        SessionRequestedAtUtc = null;
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
