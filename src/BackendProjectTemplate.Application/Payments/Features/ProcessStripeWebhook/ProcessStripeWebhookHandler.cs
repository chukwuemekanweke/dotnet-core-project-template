using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;

namespace BackendProjectTemplate.Application.Payments.Features.ProcessStripeWebhook;

public sealed class ProcessStripeWebhookHandler(
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentWebhookInbox> paymentWebhookInboxRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProcessPaymentWebhookResult> HandleAsync(
        ProcessStripeWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByKeySpecification(PaymentProviderKeys.Stripe),
                cancellationToken)
            ?? throw new InvalidOperationException("Stripe payment provider is not active.");
        var paymentProviderService = paymentProviderServices.SingleOrDefault(service =>
                string.Equals(service.ProviderKey, paymentProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new PaymentProviderResolutionException(
                $"No payment provider service is registered for '{paymentProvider.ProviderKey}'.");
        var validationResult = await paymentProviderService.ValidateWebhookAsync(
            new PaymentProviderWebhookValidationRequest(command.RawPayload),
            cancellationToken);
        var parseResult = await paymentProviderService.ParseWebhookAsync(
            new PaymentProviderWebhookParseRequest(command.RawPayload),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(parseResult.WebhookEventId))
        {
            var existingWebhook = await paymentWebhookInboxRepository.FirstOrDefaultAsync(
                new PaymentWebhookInboxByEventIdSpecification(paymentProvider.Id, parseResult.WebhookEventId),
                cancellationToken);
            if (existingWebhook is not null)
            {
                return new ProcessPaymentWebhookResult(WebhookProcessingStatus.Duplicate);
            }
        }

        var inbox = PaymentWebhookInbox.Create(
            paymentProvider.Id,
            parseResult.MerchantReference,
            parseResult.ProviderReference,
            parseResult.WebhookEventName,
            parseResult.WebhookEventId,
            command.RawPayload,
            parseResult.Metadata.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
            validationResult.SignatureValidationStatus,
            validationResult.StatusChangeReason,
            now);
        await paymentWebhookInboxRepository.AddAsync(inbox, cancellationToken);

        if (validationResult.SignatureValidationStatus == SignatureValidationStatus.Invalid)
        {
            inbox.MarkIgnored("invalid_signature", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentWebhookResult(WebhookProcessingStatus.Ignored);
        }

        var paymentTransaction = await ResolvePaymentTransactionAsync(parseResult, cancellationToken);
        if (paymentTransaction is null || parseResult.PaymentStatus is null)
        {
            inbox.MarkIgnored("transaction_not_found_or_unmapped_status", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentWebhookResult(WebhookProcessingStatus.Ignored);
        }

        var webhookStatus = await ApplyPaymentStatusAsync(paymentTransaction, parseResult, eventPublisher, now, cancellationToken);
        MarkInbox(inbox, webhookStatus, parseResult.StatusChangeReason, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProcessPaymentWebhookResult(webhookStatus);
    }

    private async Task<PaymentTransaction?> ResolvePaymentTransactionAsync(
        PaymentProviderWebhookParseResult parseResult,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(parseResult.MerchantReference))
        {
            var transactionByMerchantReference = await paymentTransactionRepository.FirstOrDefaultAsync(
                new PaymentTransactionByMerchantReferenceSpecification(parseResult.MerchantReference),
                cancellationToken);
            if (transactionByMerchantReference is not null)
            {
                return transactionByMerchantReference;
            }
        }

        if (!string.IsNullOrWhiteSpace(parseResult.ProviderReference))
        {
            return await paymentTransactionRepository.FirstOrDefaultAsync(
                new PaymentTransactionByProviderReferenceSpecification(parseResult.ProviderReference),
                cancellationToken);
        }

        return null;
    }

    private static void MarkInbox(
        PaymentWebhookInbox inbox,
        WebhookProcessingStatus webhookStatus,
        string? statusChangeReason,
        DateTimeOffset utcNow)
    {
        switch (webhookStatus)
        {
            case WebhookProcessingStatus.Processed:
                inbox.MarkProcessed(statusChangeReason, utcNow);
                break;
            case WebhookProcessingStatus.Duplicate:
                inbox.MarkDuplicate(statusChangeReason ?? "duplicate_webhook", utcNow);
                break;
            default:
                inbox.MarkIgnored(statusChangeReason, utcNow);
                break;
        }
    }

    private static async Task<WebhookProcessingStatus> ApplyPaymentStatusAsync(
        PaymentTransaction paymentTransaction,
        PaymentProviderWebhookParseResult parseResult,
        IEventPublisher eventPublisher,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (paymentTransaction.PaymentStatus == parseResult.PaymentStatus)
        {
            return WebhookProcessingStatus.Duplicate;
        }

        if (paymentTransaction.PaymentStatus == PaymentStatus.PendingInitiation)
        {
            paymentTransaction.MarkInitiatedForReconciliation();
        }

        var metadata = parseResult.Metadata.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        switch (parseResult.PaymentStatus)
        {
            case PaymentStatus.Succeeded:
                paymentTransaction.MarkSucceeded(parseResult.ProviderReference, parseResult.StatusChangeReason, metadata, utcNow);
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
                        OccuredAt = utcNow
                    },
                    cancellationToken);
                return WebhookProcessingStatus.Processed;
            case PaymentStatus.Failed:
                paymentTransaction.MarkFailed(parseResult.ProviderReference, parseResult.FailureReason, parseResult.StatusChangeReason, metadata, utcNow);
                return WebhookProcessingStatus.Processed;
            case PaymentStatus.Processing:
                paymentTransaction.MarkProcessing(parseResult.ProviderReference, parseResult.StatusChangeReason, metadata);
                return WebhookProcessingStatus.Processed;
            case PaymentStatus.AwaitingCustomerAction:
                paymentTransaction.MarkAwaitingCustomerAction(parseResult.ProviderReference, parseResult.StatusChangeReason, metadata);
                return WebhookProcessingStatus.Processed;
            default:
                return WebhookProcessingStatus.Ignored;
        }
    }
}
