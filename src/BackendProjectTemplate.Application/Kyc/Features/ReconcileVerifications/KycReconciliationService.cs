using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Application.Kyc.Features.ReconcileVerifications;

public sealed record ReconcileKycVerificationsResult(int CheckedCount, int UpdatedCount, int FailedCount);

/// <summary>
/// Polls the KYC provider for active attempts whose callbacks were missed or could not be correlated,
/// and applies the authoritative provider result through the same aggregate rules as callbacks.
/// </summary>
public sealed class KycReconciliationService(
    IRepository<KycVerification> verificationRepository,
    IRepository<Stakeholder> stakeholderRepository,
    IEnumerable<IKycProviderService> providerServices,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<KycReconciliationService> logger)
{
    public async Task<ReconcileKycVerificationsResult> HandleAsync(
        DateTimeOffset staleThresholdUtc,
        DateTimeOffset nextCheckThresholdUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var verifications = await verificationRepository.ListAsync(
            new PendingKycReconciliationSpecification(staleThresholdUtc, nextCheckThresholdUtc, batchSize),
            cancellationToken);
        if (verifications.Count == 0)
        {
            return new(0, 0, 0);
        }

        var now = timeProvider.GetUtcNow();
        var updated = 0;
        var failed = 0;
        foreach (var verification in verifications)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (await ReconcileAsync(verification, now, cancellationToken))
                {
                    updated++;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or KycProviderRequestRejectedException or InvalidOperationException ||
                                              (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                failed++;
                logger.LogWarning(
                    exception,
                    "KYC reconciliation could not fetch provider verification for attempt {KycVerificationId}.",
                    verification.Id);
            }

            // Recorded on success and failure alike so one unreachable verification backs off by the recheck delay.
            verification.RecordReconciliation(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(verifications.Count, updated, failed);
    }

    private async Task<bool> ReconcileAsync(KycVerification verification, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var provider = providerServices.SingleOrDefault(service =>
                string.Equals(service.ProviderKey, verification.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No KYC provider service is registered for '{verification.ProviderKey}'.");

        var result = await provider.GetVerificationAsync(verification.ProviderVerificationReference!, cancellationToken);
        if (!string.Equals(result.VerificationReference, verification.ProviderVerificationReference, StringComparison.Ordinal) ||
            (verification.ProviderApplicantReference is not null &&
             !string.Equals(verification.ProviderApplicantReference, result.ApplicantReference, StringComparison.Ordinal)))
        {
            logger.LogWarning(
                "KYC reconciliation ignored a provider result that does not correlate with attempt {KycVerificationId}.",
                verification.Id);
            return false;
        }

        var previousStatus = verification.Status;
        verification.ApplyProviderResult(result.ProviderStatus, result.Verified, result.ApplicantReference, now);
        if (verification.Status == KycVerificationStatus.Verified && previousStatus != KycVerificationStatus.Verified)
        {
            var stakeholder = await stakeholderRepository.GetByIdAsync(verification.StakeholderId, cancellationToken);
            if (stakeholder is not null && stakeholder.TenantId == verification.TenantId)
            {
                stakeholder.MarkVerified();
            }
        }

        return verification.Status != previousStatus;
    }
}
