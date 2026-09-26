namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

public sealed record TwoFactorProofRequest(string VerificationMethod, string Code);
