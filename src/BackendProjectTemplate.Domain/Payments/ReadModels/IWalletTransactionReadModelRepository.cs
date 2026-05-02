namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public interface IWalletTransactionReadModelRepository
{
    Task<StakeholderWalletTransactionsCursorPage> GetByStakeholderAsync(
        StakeholderWalletTransactionsCursorRequest request,
        CancellationToken cancellationToken);
}
