namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public interface IKycAdministrationReadModelRepository
{
    Task<AdminKycVerificationListPage> ListVerificationsAsync(
        AdminKycVerificationListRequest request,
        CancellationToken cancellationToken);

    Task<AdminKycVerificationDetailReadModel?> GetVerificationDetailAsync(
        Guid verificationId,
        int callbackHistoryLimit,
        CancellationToken cancellationToken);

    Task<AdminKycSummaryReadModel> GetSummaryAsync(
        AdminKycSummaryFilter filter,
        CancellationToken cancellationToken);
}
