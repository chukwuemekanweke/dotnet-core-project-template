namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record StepUpMfaProof(
    Guid AppUserId,
    Guid StakeholderId,
    Guid SessionId,
    string SecurityStamp,
    string Operation,
    string RequestFingerprint,
    DateTimeOffset VerifiedAtUtc,
    DateTimeOffset ExpiresAtUtc);
