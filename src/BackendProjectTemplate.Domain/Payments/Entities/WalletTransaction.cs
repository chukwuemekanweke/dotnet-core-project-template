using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Payments.Entities;

public sealed class WalletTransaction : Entity, IAggregateRoot
{
    private WalletTransaction()
    {
    }

    private WalletTransaction(
        Guid walletId,
        Guid paymentTransactionId,
        string merchantReference,
        WalletTransactionType transactionType,
        decimal amount,
        Guid currencyId)
    {
        WalletId = walletId;
        PaymentTransactionId = paymentTransactionId;
        MerchantReference = merchantReference.Trim();
        TransactionType = transactionType;
        Amount = amount;
        CurrencyId = currencyId;
    }

    public Guid WalletId { get; private set; }
    public Guid PaymentTransactionId { get; private set; }
    public string MerchantReference { get; private set; } = string.Empty;
    public WalletTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }

    public static WalletTransaction CreateCredit(
        Guid walletId,
        Guid paymentTransactionId,
        string merchantReference,
        decimal amount,
        Guid currencyId,
        DateTimeOffset utcNow) =>
        new(walletId, paymentTransactionId, merchantReference, WalletTransactionType.Credit, amount, currencyId);
}
