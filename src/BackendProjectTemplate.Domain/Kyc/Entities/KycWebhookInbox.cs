using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Kyc.Entities;

public sealed class KycWebhookInbox : Entity, IAggregateRoot
{
    private KycWebhookInbox()
    {
    }

    private KycWebhookInbox(
        string providerKey,
        string requestId,
        string eventType,
        string? verificationReference,
        string? sessionReference,
        string? applicantReference,
        string? externalApplicantId,
        string providerStatus,
        bool? verified,
        DateTimeOffset receivedAtUtc)
    {
        ProviderKey = providerKey;
        RequestId = requestId;
        EventType = eventType;
        VerificationReference = verificationReference;
        SessionReference = sessionReference;
        ApplicantReference = applicantReference;
        ExternalApplicantId = externalApplicantId;
        ProviderStatus = providerStatus;
        Verified = verified;
        ReceivedAtUtc = receivedAtUtc;
    }

    public string ProviderKey { get; private set; } = string.Empty;
    public string RequestId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string? VerificationReference { get; private set; }
    public string? SessionReference { get; private set; }
    public string? ApplicantReference { get; private set; }
    public string? ExternalApplicantId { get; private set; }
    public string ProviderStatus { get; private set; } = string.Empty;
    public bool? Verified { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public static KycWebhookInbox Create(
        string providerKey,
        string requestId,
        string eventType,
        string? verificationReference,
        string? sessionReference,
        string? applicantReference,
        string? externalApplicantId,
        string providerStatus,
        bool? verified,
        DateTimeOffset receivedAtUtc) =>
        new(
            providerKey.Trim(),
            requestId.Trim(),
            eventType.Trim(),
            NormalizeOptional(verificationReference),
            NormalizeOptional(sessionReference),
            NormalizeOptional(applicantReference),
            NormalizeOptional(externalApplicantId),
            providerStatus.Trim(),
            verified,
            receivedAtUtc);

    public void MarkProcessed(DateTimeOffset utcNow) => ProcessedAtUtc = utcNow;

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
