namespace BackendProjectTemplate.Domain.Kyc;

public sealed record KycSessionRequest(string ExternalApplicantId, string? ApplicantReference);
