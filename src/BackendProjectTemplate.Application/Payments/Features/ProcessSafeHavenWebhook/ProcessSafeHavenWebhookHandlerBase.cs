using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;

namespace BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook;

public abstract class ProcessSafeHavenWebhookHandlerBase<TData>(
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentWebhookInbox> paymentWebhookInboxRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProcessPaymentWebhookResult> HandleAsync(
        ProcessSafeHavenWebhookCommand<TData> command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByKeySpecification(PaymentProviderKeys.SafeHaven),
                cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven payment provider is not active.");
        var validationResult = new PaymentProviderWebhookValidationResult(
            SignatureValidationStatus.NotApplicable,
            "signature_not_applicable");
        var webhookDetails = CreateWebhookDetails(command.Webhook);

        if (!string.IsNullOrWhiteSpace(webhookDetails.WebhookEventId))
        {
            var existingWebhook = await paymentWebhookInboxRepository.FirstOrDefaultAsync(
                new PaymentWebhookInboxByEventIdSpecification(paymentProvider.Id, webhookDetails.WebhookEventId),
                cancellationToken);
            if (existingWebhook is not null)
            {
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Payments.WebhookDuplicate,
                    ObservabilityEventProperties.CreatePayment(
                        Observability.StepNames.WebhookReceipt,
                        Observability.Outcomes.Duplicate,
                        provider: paymentProvider.ProviderKey,
                        paymentReference: webhookDetails.MerchantReference,
                        source: Observability.Sources.Webhook,
                        isDuplicate: true));
                return new ProcessPaymentWebhookResult(WebhookReceiptStatus.Duplicate);
            }
        }

        var inbox = PaymentWebhookInbox.Create(
            paymentProvider.Id,
            webhookDetails.MerchantReference,
            webhookDetails.ProviderReference,
            webhookDetails.WebhookEventName,
            webhookDetails.WebhookEventId,
            command.RawPayload,
            validationResult.SignatureValidationStatus,
            validationResult.StatusChangeReason,
            now);
        await paymentWebhookInboxRepository.AddAsync(inbox, cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.WebhookReceived,
            ObservabilityEventProperties.CreatePayment(
                Observability.StepNames.WebhookReceipt,
                Observability.Outcomes.Success,
                provider: paymentProvider.ProviderKey,
                paymentReference: webhookDetails.MerchantReference,
                source: Observability.Sources.Webhook));

        if (validationResult.SignatureValidationStatus == SignatureValidationStatus.Invalid)
        {
            inbox.MarkIgnored("invalid_signature", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.WebhookInvalidSignature,
                ObservabilityEventProperties.CreatePayment(
                    Observability.StepNames.WebhookProcessing,
                    Observability.Outcomes.Failure,
                    failureReason: ObservabilityFailureReasons.InvalidSignature,
                    provider: paymentProvider.ProviderKey,
                    paymentReference: webhookDetails.MerchantReference,
                    source: Observability.Sources.Webhook));
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.InvalidSignature);
        }

        var paymentTransaction = await ResolvePaymentTransactionAsync(webhookDetails, cancellationToken);
        if (paymentTransaction is null || !webhookDetails.IsSupportedEvent)
        {
            inbox.MarkIgnored("transaction_not_found_or_unmapped_status", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.WebhookProcessingFailed,
                ObservabilityEventProperties.CreatePayment(
                    Observability.StepNames.WebhookProcessing,
                    Observability.Outcomes.Ignored,
                    failureReason: ObservabilityFailureReasons.TransactionNotFoundOrUnmappedStatus,
                    provider: paymentProvider.ProviderKey,
                    paymentReference: webhookDetails.MerchantReference,
                    source: Observability.Sources.Webhook));
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.UnidentifiedTransaction);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.WebhookProcessed,
            ObservabilityEventProperties.CreatePayment(
                Observability.StepNames.WebhookProcessing,
                Observability.Outcomes.Success,
                paymentTransaction.StakeholderId,
                provider: paymentProvider.ProviderKey,
                paymentReference: paymentTransaction.MerchantReference,
                paymentIntent: paymentTransaction.PaymentIntent,
                amount: paymentTransaction.Amount,
                currencyId: paymentTransaction.CurrencyId,
                source: Observability.Sources.Webhook));

        return new ProcessPaymentWebhookResult(WebhookReceiptStatus.Persisted);
    }

    protected abstract SafeHavenWebhookDetails CreateWebhookDetails(SafeHavenWebhook<TData> webhook);

    protected static string GetRequiredEventName(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("SafeHaven webhook event name is required.")
            : value.Trim();

    protected static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected static string? CreateWebhookEventId(string? merchantReference, string eventName) =>
        string.IsNullOrWhiteSpace(merchantReference) ? null : $"{merchantReference.Trim()}:{eventName}";

    private async Task<PaymentTransaction?> ResolvePaymentTransactionAsync(
        SafeHavenWebhookDetails webhookDetails,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(webhookDetails.MerchantReference))
        {
            var transactionByMerchantReference = await paymentTransactionRepository.FirstOrDefaultAsync(
                new PaymentTransactionByMerchantReferenceSpecification(webhookDetails.MerchantReference),
                cancellationToken);
            if (transactionByMerchantReference is not null)
            {
                return transactionByMerchantReference;
            }
        }

        if (!string.IsNullOrWhiteSpace(webhookDetails.ProviderReference))
        {
            return await paymentTransactionRepository.FirstOrDefaultAsync(
                new PaymentTransactionByProviderReferenceSpecification(webhookDetails.ProviderReference),
                cancellationToken);
        }

        return null;
    }
}
