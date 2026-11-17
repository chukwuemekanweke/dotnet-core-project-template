namespace BackendProjectTemplate.Domain.Kyc;

public sealed record KycProviderVerificationResult(
    string VerificationReference,
    string? ApplicantReference,
    KycVerificationTransition Transition);
