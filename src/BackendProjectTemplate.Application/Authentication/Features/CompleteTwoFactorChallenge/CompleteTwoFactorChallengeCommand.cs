using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.CompleteTwoFactorChallenge;

public sealed record CompleteTwoFactorChallengeCommand(
    string Challenge,
    TwoFactorVerificationMethod VerificationMethod,
    string Code);
