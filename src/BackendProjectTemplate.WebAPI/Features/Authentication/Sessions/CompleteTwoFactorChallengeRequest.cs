namespace BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;

public sealed record CompleteTwoFactorChallengeRequest(string Challenge, string VerificationMethod, string Code);
