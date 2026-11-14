using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;

namespace BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;

public sealed class ProcessBachsWebhookHandler(
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentWebhookInbox> paymentWebhookInboxRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<Currency> currencyRepository,
    IBachsWebhookSignatureValidator signatureValidator,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    IEventPublisher eventPublisher,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly ActorContext AnonymousActorContext = new(null, null, string.Empty, string.Empty);

    public async Task<ProcessPaymentWebhookResult> HandleAsync(
        ProcessBachsWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByKeySpecification(PaymentProviderKeys.Bachs),
                cancellationToken)
            ?? throw new InvalidOperationException("Bachs payment provider is not active.");
        var validation = await signatureValidator.ValidateAsync(
            new BachsWebhookSignatureValidationRequest(
                command.RawPayloadBytes,
                command.SignatureV2Header,
                command.SignatureHeader,
                command.TimestampHeader),
            cancellationToken);

        var eventId = NormalizeRequired(command.Webhook.Id, "Bachs webhook event id is required.");
        var eventName = NormalizeRequired(command.Webhook.Type, "Bachs webhook event type is required.");
        var merchantReference = NormalizeOptional(command.Webhook.Data.Reference);
        var providerReference = NormalizeOptional(command.Webhook.Data.CheckoutId);

        var existingWebhook = await paymentWebhookInboxRepository.FirstOrDefaultAsync(
            new PaymentWebhookInboxByEventIdSpecification(paymentProvider.Id, eventId),
            cancellationToken);
        if (existingWebhook is not null)
        {
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.Duplicate);
        }

        var inbox = PaymentWebhookInbox.Create(
            paymentProvider.Id,
            merchantReference,
            providerReference,
            eventName,
            eventId,
            command.RawPayload,
            validation.SignatureValidationStatus,
            validation.StatusChangeReason,
            now);
        await paymentWebhookInboxRepository.AddAsync(inbox, cancellationToken);

        if (validation.SignatureValidationStatus != SignatureValidationStatus.Valid)
        {
            inbox.MarkIgnored(KnownWebhookStatusChangeReasons.Shared.InvalidSignature, now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.InvalidSignature);
        }

        if (!IsSupportedEvent(eventName))
        {
            inbox.MarkIgnored(KnownWebhookStatusChangeReasons.Payments.TransactionNotFoundOrUnmappedStatus, now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.UnidentifiedTransaction);
        }

        var paymentTransaction = await ResolvePaymentTransactionAsync(
            merchantReference,
            providerReference,
            cancellationToken);
        if (paymentTransaction is null)
        {
            inbox.MarkIgnored(KnownWebhookStatusChangeReasons.Payments.TransactionNotFoundOrUnmappedStatus, now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.UnidentifiedTransaction);
        }

        if (paymentTransaction.HasReachedTerminalState())
        {
            inbox.MarkDuplicate("payment_transaction_already_terminal", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.Duplicate);
        }

        var currency = await currencyRepository.GetByIdAsync(paymentTransaction.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Currency '{paymentTransaction.CurrencyId}' was not found for payment transaction '{paymentTransaction.Id}'.");
        var providerService = paymentProviderServices.SingleOrDefault(service =>
                string.Equals(service.ProviderKey, PaymentProviderKeys.Bachs, StringComparison.OrdinalIgnoreCase))
            ?? throw new PaymentProviderResolutionException("No Bachs payment provider service is registered.");
        var verification = await providerService.VerifyPaymentAsync(
            new PaymentProviderVerificationRequest(
                paymentTransaction.MerchantReference,
                paymentTransaction.ProviderReference,
                paymentTransaction.Amount,
                currency.CurrencyCode,
                paymentTransaction.PaymentIntent),
            cancellationToken);

        await ApplyVerificationAsync(paymentTransaction, verification, now, cancellationToken);
        inbox.MarkProcessed(verification.StatusChangeReason, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.WebhookPersisted,
            ObservabilityEventProperties.Create(
                AnonymousActorContext,
                paymentTransaction.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.Provider] = PaymentProviderKeys.Bachs,
                    [Observability.PropertyNames.Payments.MerchantReference] = paymentTransaction.MerchantReference,
                    [Observability.PropertyNames.Payments.ProviderReference] = paymentTransaction.ProviderReference ?? string.Empty
                }));

        return new ProcessPaymentWebhookResult(WebhookReceiptStatus.Persisted);
    }

    private async Task ApplyVerificationAsync(
        PaymentTransaction paymentTransaction,
        PaymentProviderVerificationResult verification,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        switch (verification.VerificationStatus)
        {
            case PaymentProviderVerificationStatus.Succeeded:
                paymentTransaction.MarkSucceeded(verification.ProviderReference, verification.StatusChangeReason, now);
                await eventPublisher.PublishAsync(
                    new SuccessfulPaymentConfirmed
                    {
                        PaymentTransactionId = paymentTransaction.Id,
                        MerchantReference = paymentTransaction.MerchantReference,
                        PaymentIntent = paymentTransaction.PaymentIntent,
                        PaymentProviderId = paymentTransaction.PaymentProviderId,
                        Amount = paymentTransaction.Amount,
                        CurrencyId = paymentTransaction.CurrencyId,
                        StakeholderId = paymentTransaction.StakeholderId,
                        TenantId = paymentTransaction.TenantId,
                        FlowId = string.Empty,
                        OccuredAt = now
                    },
                    cancellationToken);
                break;
            case PaymentProviderVerificationStatus.Failed:
                paymentTransaction.MarkFailed(
                    verification.ProviderReference,
                    verification.FailureReason,
                    verification.StatusChangeReason,
                    now);
                break;
            case PaymentProviderVerificationStatus.Expired:
                paymentTransaction.MarkExpired(
                    verification.ProviderReference,
                    verification.FailureReason,
                    verification.StatusChangeReason);
                break;
            case PaymentProviderVerificationStatus.Processing:
                paymentTransaction.RecordStatusCheck(now);
                break;
        }
    }

    private async Task<PaymentTransaction?> ResolvePaymentTransactionAsync(
        string? merchantReference,
        string? providerReference,
        CancellationToken cancellationToken)
    {
        if (merchantReference is not null)
        {
            var transaction = await paymentTransactionRepository.FirstOrDefaultAsync(
                new PaymentTransactionByMerchantReferenceSpecification(merchantReference),
                cancellationToken);
            if (transaction is not null)
            {
                return transaction;
            }
        }

        return providerReference is null
            ? null
            : await paymentTransactionRepository.FirstOrDefaultAsync(
                new PaymentTransactionByProviderReferenceSpecification(providerReference),
                cancellationToken);
    }

    private static bool IsSupportedEvent(string eventName) =>
        eventName is BachsWebhookEvents.CollectionSucceeded or
            BachsWebhookEvents.CollectionFailed or
            BachsWebhookEvents.CheckoutExpired;

    private static string NormalizeRequired(string? value, string errorMessage) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException(errorMessage) : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
