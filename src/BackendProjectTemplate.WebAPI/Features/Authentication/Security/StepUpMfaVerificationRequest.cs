namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

public sealed record StepUpMfaVerificationRequest(
    string VerificationMethod,
    string Code,
    string Operation,
    StepUpMfaIntendedRequest IntendedRequest);
