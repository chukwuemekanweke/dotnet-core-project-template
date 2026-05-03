using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
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
    private static readonly ActorContext AnonymousActorContext = new(null, null, string.Empty, string.Empty);

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
                    Observability.EventNames.Payments.WebhookPersistenceFailed,
                    ObservabilityEventProperties.Create(
                        AnonymousActorContext,
                        failureReason: ObservabilityFailureReasons.DuplicateProcessing,
                        additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, webhookDetails.MerchantReference)));
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
            ObservabilityEventProperties.Create(
                AnonymousActorContext,
                additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, webhookDetails.MerchantReference)));

        if (validationResult.SignatureValidationStatus == SignatureValidationStatus.Invalid)
        {
            inbox.MarkIgnored("invalid_signature", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.WebhookPersistenceFailed,
                ObservabilityEventProperties.Create(
                    AnonymousActorContext,
                    failureReason: ObservabilityFailureReasons.InvalidSignature,
                    additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, webhookDetails.MerchantReference)));
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.InvalidSignature);
        }

        var paymentTransaction = await ResolvePaymentTransactionAsync(webhookDetails, cancellationToken);
        if (paymentTransaction is null || !webhookDetails.IsSupportedEvent)
        {
            inbox.MarkIgnored("transaction_not_found_or_unmapped_status", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.WebhookPersistenceFailed,
                ObservabilityEventProperties.Create(
                    AnonymousActorContext,
                    failureReason: ObservabilityFailureReasons.TransactionNotFoundOrUnmappedStatus,
                    additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, webhookDetails.MerchantReference)));
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.UnidentifiedTransaction);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.WebhookPersisted,
            ObservabilityEventProperties.Create(
                AnonymousActorContext,
                paymentTransaction.StakeholderId,
                additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, paymentTransaction.MerchantReference)));

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

    private static Dictionary<string, string> CreateWebhookProperties(string provider, string? paymentReference) =>
        string.IsNullOrWhiteSpace(paymentReference)
            ? new Dictionary<string, string>
            {
                [Observability.ProviderPropertyName] = provider
            }
            : new Dictionary<string, string>
            {
                [Observability.ProviderPropertyName] = provider,
                [Observability.PaymentReferencePropertyName] = paymentReference
            };

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
