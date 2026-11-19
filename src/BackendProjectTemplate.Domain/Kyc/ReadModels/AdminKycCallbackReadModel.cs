namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycCallbackReadModel(
    string RequestId,
    string EventType,
    string ProviderStatus,
    bool? Verified,
    KycWebhookProcessingOutcome ProcessingOutcome,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? ProcessedAtUtc);
