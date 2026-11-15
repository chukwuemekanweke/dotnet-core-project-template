using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Kyc.Entities;

namespace BackendProjectTemplate.Domain.Kyc.Specifications;

public sealed class ActiveKycVerificationSpecification : Specification<KycVerification>
{
    public ActiveKycVerificationSpecification(Guid tenantId, Guid stakeholderId)
    {
        Where(value => value.TenantId == tenantId && value.StakeholderId == stakeholderId &&
            (value.Status == KycVerificationStatus.Pending || value.Status == KycVerificationStatus.InProgress));
        ApplyOrderByDescending(value => value.StartedAtUtc);
        EnableTracking();
    }
}

public sealed class CurrentKycVerificationSpecification : Specification<KycVerification>
{
    public CurrentKycVerificationSpecification(Guid tenantId, Guid stakeholderId, bool track = false)
    {
        Where(value => value.TenantId == tenantId && value.StakeholderId == stakeholderId);
        ApplyOrderByDescending(value => value.StartedAtUtc);
        if (track) EnableTracking();
    }
}

public sealed class KycVerificationByProviderReferenceSpecification : Specification<KycVerification>
{
    public KycVerificationByProviderReferenceSpecification(string providerKey, string verificationReference)
    {
        Where(value => value.ProviderKey == providerKey && value.ProviderVerificationReference == verificationReference);
        EnableTracking();
    }
}

public sealed class KycWebhookByRequestIdSpecification : Specification<KycWebhookInbox>
{
    public KycWebhookByRequestIdSpecification(string providerKey, string requestId) =>
        Where(value => value.ProviderKey == providerKey && value.RequestId == requestId);
}

public sealed class KycVerificationBySessionReferenceSpecification : Specification<KycVerification>
{
    public KycVerificationBySessionReferenceSpecification(string providerKey, string sessionReference)
    {
        Where(value => value.ProviderKey == providerKey && value.ProviderSessionReference == sessionReference);
        EnableTracking();
    }
}

public sealed class PendingKycReconciliationSpecification : Specification<KycVerification>
{
    public PendingKycReconciliationSpecification(
        DateTimeOffset staleThresholdUtc,
        DateTimeOffset nextCheckThresholdUtc,
        int batchSize)
    {
        Where(value => value.ProviderVerificationReference != null &&
            (value.Status == KycVerificationStatus.Pending || value.Status == KycVerificationStatus.InProgress) &&
            value.StartedAtUtc <= staleThresholdUtc &&
            (!value.LastReconciledAtUtc.HasValue || value.LastReconciledAtUtc <= nextCheckThresholdUtc));
        ApplyOrderBy(value => value.StartedAtUtc);
        ApplyPaging(0, batchSize);
        EnableTracking();
    }
}
