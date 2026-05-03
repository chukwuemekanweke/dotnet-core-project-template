using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
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
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ReconcilePaymentsResult> HandleAsync(
        DateTimeOffset oldestEligibleCreatedAtUtc,
        DateTimeOffset staleThresholdUtc,
        DateTimeOffset nextCheckThresholdUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var transactions = await paymentTransactionRepository.ListAsync(
            new PendingPaymentReconciliationSpecification(
                oldestEligibleCreatedAtUtc,
                staleThresholdUtc,
                nextCheckThresholdUtc,
                batchSize),
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

            if (!transaction.LastStatusCheckAtUtc.HasValue)
            {
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Payments.TimeoutNoWebhook,
                    ObservabilityEventProperties.CreatePayment(
                        Observability.StepNames.PaymentReconciliation,
                        Observability.Outcomes.Timeout,
                        transaction.StakeholderId,
                        provider: paymentProvider.ProviderKey,
                        paymentReference: transaction.MerchantReference,
                        paymentMethodType: transaction.PaymentMethodType,
                        paymentIntent: transaction.PaymentIntent,
                        amount: transaction.Amount,
                        currencyId: transaction.CurrencyId,
                        source: Observability.Sources.Cron));
            }

            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.ReconciliationChecked,
                ObservabilityEventProperties.CreatePayment(
                    Observability.StepNames.PaymentReconciliation,
                    Observability.Outcomes.Success,
                    transaction.StakeholderId,
                    provider: paymentProvider.ProviderKey,
                    paymentReference: transaction.MerchantReference,
                    paymentMethodType: transaction.PaymentMethodType,
                    paymentIntent: transaction.PaymentIntent,
                    amount: transaction.Amount,
                    currencyId: transaction.CurrencyId,
                    source: Observability.Sources.Cron));

            var verificationResult = await paymentProviderService.VerifyPaymentAsync(
                new PaymentProviderVerificationRequest(
                    transaction.MerchantReference,
                    transaction.ProviderReference,
                    transaction.Amount,
                    currency.CurrencyCode,
                    transaction.PaymentIntent),
                cancellationToken);

            switch (verificationResult.VerificationStatus)
            {
                case PaymentProviderVerificationStatus.Succeeded when transaction.PaymentStatus != Contracts.Payments.PaymentStatus.Succeeded:
                    transaction.MarkSucceeded(verificationResult.ProviderReference, verificationResult.StatusChangeReason, now);
                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.StatusConfirmed,
                        ObservabilityEventProperties.CreatePayment(
                            Observability.StepNames.PaymentStatusConfirmation,
                            Observability.Outcomes.Success,
                            transaction.StakeholderId,
                            provider: paymentProvider.ProviderKey,
                            paymentReference: transaction.MerchantReference,
                            paymentMethodType: transaction.PaymentMethodType,
                            paymentIntent: transaction.PaymentIntent,
                            amount: transaction.Amount,
                            currencyId: transaction.CurrencyId,
                            source: Observability.Sources.Cron,
                            terminalState: Contracts.Payments.PaymentStatus.Succeeded.ToString()));

                    try
                    {
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
                        customTelemetryContext.AddCustomEvent(
                            Observability.EventNames.Payments.EventPublished,
                            ObservabilityEventProperties.CreatePayment(
                                Observability.StepNames.PaymentEventPublish,
                                Observability.Outcomes.Success,
                                transaction.StakeholderId,
                                provider: paymentProvider.ProviderKey,
                                paymentReference: transaction.MerchantReference,
                                paymentMethodType: transaction.PaymentMethodType,
                                paymentIntent: transaction.PaymentIntent,
                                amount: transaction.Amount,
                                currencyId: transaction.CurrencyId,
                                source: Observability.Sources.Cron));
                    }
                    catch (Exception ex)
                    {
                        customTelemetryContext.AddCustomEvent(
                            Observability.EventNames.Payments.EventPublishFailed,
                            ObservabilityEventProperties.CreatePayment(
                                Observability.StepNames.PaymentEventPublish,
                                Observability.Outcomes.Failure,
                                transaction.StakeholderId,
                                ex.GetType().Name,
                                paymentProvider.ProviderKey,
                                transaction.MerchantReference,
                                transaction.PaymentMethodType,
                                transaction.PaymentIntent,
                                transaction.Amount,
                                transaction.CurrencyId,
                                Observability.Sources.Cron,
                                exceptionType: ex.GetType().Name));
                        throw;
                    }

                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.ReconciliationConfirmed,
                        ObservabilityEventProperties.CreatePayment(
                            Observability.StepNames.PaymentReconciliation,
                            Observability.Outcomes.Success,
                            transaction.StakeholderId,
                            provider: paymentProvider.ProviderKey,
                            paymentReference: transaction.MerchantReference,
                            paymentMethodType: transaction.PaymentMethodType,
                            paymentIntent: transaction.PaymentIntent,
                            amount: transaction.Amount,
                            currencyId: transaction.CurrencyId,
                            source: Observability.Sources.Cron,
                            terminalState: Contracts.Payments.PaymentStatus.Succeeded.ToString()));
                    break;
                case PaymentProviderVerificationStatus.Failed when transaction.PaymentStatus != Contracts.Payments.PaymentStatus.Failed:
                    transaction.MarkFailed(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason,
                        now);
                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.StatusFailed,
                        ObservabilityEventProperties.CreatePayment(
                            Observability.StepNames.PaymentStatusConfirmation,
                            Observability.Outcomes.Failure,
                            transaction.StakeholderId,
                            verificationResult.FailureReason,
                            paymentProvider.ProviderKey,
                            transaction.MerchantReference,
                            transaction.PaymentMethodType,
                            transaction.PaymentIntent,
                            transaction.Amount,
                            transaction.CurrencyId,
                            Observability.Sources.Cron,
                            Contracts.Payments.PaymentStatus.Failed.ToString()));
                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.ReconciliationFailed,
                        ObservabilityEventProperties.CreatePayment(
                            Observability.StepNames.PaymentReconciliation,
                            Observability.Outcomes.Failure,
                            transaction.StakeholderId,
                            verificationResult.FailureReason,
                            paymentProvider.ProviderKey,
                            transaction.MerchantReference,
                            transaction.PaymentMethodType,
                            transaction.PaymentIntent,
                            transaction.Amount,
                            transaction.CurrencyId,
                            Observability.Sources.Cron,
                            Contracts.Payments.PaymentStatus.Failed.ToString()));
                    break;
                case PaymentProviderVerificationStatus.Expired when transaction.PaymentStatus != Contracts.Payments.PaymentStatus.Expired:
                    transaction.MarkExpired(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason);
                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.StatusFailed,
                        ObservabilityEventProperties.CreatePayment(
                            Observability.StepNames.PaymentStatusConfirmation,
                            Observability.Outcomes.Failure,
                            transaction.StakeholderId,
                            verificationResult.FailureReason,
                            paymentProvider.ProviderKey,
                            transaction.MerchantReference,
                            transaction.PaymentMethodType,
                            transaction.PaymentIntent,
                            transaction.Amount,
                            transaction.CurrencyId,
                            Observability.Sources.Cron,
                            Contracts.Payments.PaymentStatus.Expired.ToString()));
                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.ReconciliationFailed,
                        ObservabilityEventProperties.CreatePayment(
                            Observability.StepNames.PaymentReconciliation,
                            Observability.Outcomes.Failure,
                            transaction.StakeholderId,
                            verificationResult.FailureReason,
                            paymentProvider.ProviderKey,
                            transaction.MerchantReference,
                            transaction.PaymentMethodType,
                            transaction.PaymentIntent,
                            transaction.Amount,
                            transaction.CurrencyId,
                            Observability.Sources.Cron,
                            Contracts.Payments.PaymentStatus.Expired.ToString()));
                    break;
                case PaymentProviderVerificationStatus.Processing:
                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.ReconciliationStillPending,
                        ObservabilityEventProperties.CreatePayment(
                            Observability.StepNames.PaymentReconciliation,
                            Observability.Outcomes.Retry,
                            transaction.StakeholderId,
                            provider: paymentProvider.ProviderKey,
                            paymentReference: transaction.MerchantReference,
                            paymentMethodType: transaction.PaymentMethodType,
                            paymentIntent: transaction.PaymentIntent,
                            amount: transaction.Amount,
                            currencyId: transaction.CurrencyId,
                            source: Observability.Sources.Cron));
                    break;
            }

            transaction.RecordStatusCheck(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReconcilePaymentsResult(transactions.Count);
    }
}
