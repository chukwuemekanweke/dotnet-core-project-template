namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTopUpTransactionDetailRequest(
    Guid StakeholderId,
    Guid WalletTransactionId);
