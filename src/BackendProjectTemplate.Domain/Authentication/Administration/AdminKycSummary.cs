using BackendProjectTemplate.Domain.Kyc;

namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminKycSummary(
    Guid VerificationId,
    KycVerificationStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
