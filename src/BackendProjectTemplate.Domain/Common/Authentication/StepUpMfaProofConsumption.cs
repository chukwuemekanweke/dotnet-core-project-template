namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record StepUpMfaProofConsumption(
    Guid AppUserId,
    Guid StakeholderId,
    Guid SessionId,
    string SecurityStamp,
    string Operation,
    string RequestFingerprint);
