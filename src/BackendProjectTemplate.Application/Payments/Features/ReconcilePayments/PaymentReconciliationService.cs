using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;

namespace BackendProjectTemplate.Application.Payments.Features.ReconcilePayments;

public sealed class PaymentReconciliationService(
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<Currency> currencyRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ReconcilePaymentsResult> HandleAsync(
        DateTimeOffset staleThresholdUtc,
        DateTimeOffset nextCheckThresholdUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var transactions = await paymentTransactionRepository.ListAsync(
            new PendingPaymentReconciliationSpecification(staleThresholdUtc, nextCheckThresholdUtc, batchSize),
            cancellationToken);
        if (transactions.Count == 0)
        {
            return new ReconcilePaymentsResult(0);
        }

        var now = timeProvider.GetUtcNow();
        foreach (var transaction in transactions)
        {
            var currency = await currencyRepository.GetByIdAsync(transaction.CurrencyId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Currency '{transaction.CurrencyId}' was not found for payment transaction '{transaction.Id}'.");
            var paymentProvider = await paymentProviderRepository.GetByIdAsync(transaction.PaymentProviderId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Payment provider '{transaction.PaymentProviderId}' was not found for payment transaction '{transaction.Id}'.");
            var paymentProviderService = paymentProviderServices.SingleOrDefault(service =>
                    string.Equals(service.ProviderKey, paymentProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
                ?? throw new PaymentProviderResolutionException(
                    $"No payment provider service is registered for '{paymentProvider.ProviderKey}'.");
            var verificationResult = await paymentProviderService.VerifyPaymentAsync(
                new PaymentProviderVerificationRequest(
                    transaction.MerchantReference,
                    transaction.ProviderReference,
                    transaction.Amount,
                    currency.CurrencyCode,
                    transaction.PaymentIntent),
                cancellationToken);

            if (transaction.PaymentStatus == verificationResult.PaymentStatus)
            {
                transaction.RecordStatusCheck(now);
                continue;
            }

            if (transaction.PaymentStatus == PaymentStatus.PendingInitiation)
            {
                transaction.MarkInitiatedForReconciliation();
            }

            var metadata = verificationResult.Metadata.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            switch (verificationResult.PaymentStatus)
            {
                case PaymentStatus.Succeeded:
                    transaction.MarkSucceeded(verificationResult.ProviderReference, verificationResult.StatusChangeReason, metadata, now);
                    await eventPublisher.PublishAsync(
                        new SuccessfulPaymentConfirmed
                        {
                            PaymentTransactionId = transaction.Id,
                            MerchantReference = transaction.MerchantReference,
                            PaymentIntent = transaction.PaymentIntent,
                            PaymentProviderId = transaction.PaymentProviderId,
                            Amount = transaction.Amount,
                            CurrencyId = transaction.CurrencyId,
                            StakeholderId = transaction.StakeholderId,
                            TenantId = transaction.TenantId,
                            FlowId = string.Empty,
                            OccuredAt = now
                        },
                        cancellationToken);
                    break;
                case PaymentStatus.Failed:
                    transaction.MarkFailed(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason,
                        metadata,
                        now);
                    break;
                case PaymentStatus.Processing:
                    transaction.MarkProcessing(verificationResult.ProviderReference, verificationResult.StatusChangeReason, metadata);
                    break;
                case PaymentStatus.AwaitingCustomerAction:
                    transaction.MarkAwaitingCustomerAction(verificationResult.ProviderReference, verificationResult.StatusChangeReason, metadata);
                    break;
            }

            transaction.RecordStatusCheck(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReconcilePaymentsResult(transactions.Count);
    }
}
