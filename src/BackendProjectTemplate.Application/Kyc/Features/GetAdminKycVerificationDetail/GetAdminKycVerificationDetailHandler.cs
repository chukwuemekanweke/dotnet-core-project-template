using BackendProjectTemplate.Domain.Kyc.ReadModels;

namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;

public sealed class GetAdminKycVerificationDetailHandler(IKycAdministrationReadModelRepository readModelRepository)
{
    private const int CallbackHistoryLimit = 50;

    public async Task<GetAdminKycVerificationDetailResponse?> HandleAsync(
        GetAdminKycVerificationDetailCommand command,
        CancellationToken cancellationToken)
    {
        var detail = await readModelRepository.GetVerificationDetailAsync(
            command.VerificationId,
            CallbackHistoryLimit,
            cancellationToken);
        if (detail is null)
        {
            return null;
        }

        return new GetAdminKycVerificationDetailResponse(
            new AdminKycVerificationResponse(
                detail.Verification.VerificationId,
                detail.Verification.TenantId,
                detail.Verification.StakeholderId,
                detail.Verification.ProviderKey,
                detail.Verification.Status.ToString(),
                detail.Verification.LastProviderTransition.ToString(),
                detail.Verification.StartedAtUtc,
                detail.Verification.SubmittedAtUtc,
                detail.Verification.CompletedAtUtc,
                detail.Verification.LastReconciledAtUtc),
            new AdminKycStakeholderResponse(
                detail.Stakeholder.StakeholderId,
                detail.Stakeholder.DisplayName,
                detail.Stakeholder.IsVerified),
            new AdminKycProviderResponse(
                detail.Provider.ProviderKey,
                detail.Provider.ProviderApplicantReference,
                detail.Provider.ProviderVerificationReference,
                detail.Provider.ProviderSessionReference,
                detail.Provider.LatestProviderStatus,
                detail.Provider.LatestProviderStatusObservedAtUtc),
            detail.CallbackHistory.Select(callback => new AdminKycCallbackResponse(
                callback.RequestId,
                callback.EventType,
                callback.ProviderStatus,
                callback.Verified,
                callback.ProcessingOutcome.ToString(),
                callback.ReceivedAtUtc,
                callback.ProcessedAtUtc)).ToArray());
    }
}
