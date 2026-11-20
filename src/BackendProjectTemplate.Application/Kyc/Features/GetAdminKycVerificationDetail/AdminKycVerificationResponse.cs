namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;

public sealed record AdminKycVerificationResponse(
    Guid VerificationId,
    Guid TenantId,
    Guid StakeholderId,
    string ProviderKey,
    string Status,
    string LastProviderTransition,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? LastReconciledAtUtc);
