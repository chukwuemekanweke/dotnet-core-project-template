namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycVerificationDetail;

public sealed record GetAdminKycVerificationDetailResponse(
    AdminKycVerificationResponse Verification,
    AdminKycStakeholderResponse Stakeholder,
    AdminKycProviderResponse Provider,
    IReadOnlyList<AdminKycCallbackResponse> CallbackHistory);
