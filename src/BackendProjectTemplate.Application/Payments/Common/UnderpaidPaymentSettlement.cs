using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;

namespace BackendProjectTemplate.Application.Payments.Common;

public static class UnderpaidPaymentSettlement
{
    // A wallet top-up is credited with what was actually collected. Any other intent (a subscription) is not
    // fulfilled: the transaction stays Underpaid for an administrator to refund or settle with the provider.
    public static async Task ApplyAsync(
        PaymentTransaction transaction,
        PaymentProviderVerificationResult verification,
        IEventPublisher eventPublisher,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var amountPaid = verification.AmountPaid
            ?? throw new InvalidOperationException(
                $"Underpaid verification for payment transaction '{transaction.MerchantReference}' has no amount paid.");

        transaction.MarkUnderpaid(verification.ProviderReference, amountPaid, verification.StatusChangeReason, now);

        if (transaction.PaymentIntent != PaymentIntent.WalletTopUp)
        {
            return;
        }

        await eventPublisher.PublishAsync(
            new SuccessfulPaymentConfirmed
            {
                PaymentTransactionId = transaction.Id,
                MerchantReference = transaction.MerchantReference,
                PaymentIntent = transaction.PaymentIntent,
                PaymentProviderId = transaction.PaymentProviderId,
                Amount = amountPaid,
                CurrencyId = transaction.CurrencyId,
                StakeholderId = transaction.StakeholderId,
                TenantId = transaction.TenantId,
                FlowId = string.Empty,
                OccuredAt = now
            },
            cancellationToken);
    }
}
