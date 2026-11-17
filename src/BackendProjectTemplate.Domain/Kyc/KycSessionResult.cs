namespace BackendProjectTemplate.Domain.Kyc;

public sealed record KycSessionResult(
    string? VerificationReference,
    string SessionReference,
    Uri VerificationUrl,
    int? VerificationAttemptsLeft);
