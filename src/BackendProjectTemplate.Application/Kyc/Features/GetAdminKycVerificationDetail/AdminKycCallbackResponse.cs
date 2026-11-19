namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;

public sealed record AdminKycCallbackResponse(
    string RequestId,
    string EventType,
    string ProviderStatus,
    bool? Verified,
    string ProcessingOutcome,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? ProcessedAtUtc);
