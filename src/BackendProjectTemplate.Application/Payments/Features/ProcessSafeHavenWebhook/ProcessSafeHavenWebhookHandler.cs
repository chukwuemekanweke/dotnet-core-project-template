using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;

namespace BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook;

public sealed class ProcessSafeHavenWebhookHandler(
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentWebhookInbox> paymentWebhookInboxRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProcessPaymentWebhookResult> HandleAsync<TData>(
        ProcessSafeHavenWebhookCommand<TData> command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByKeySpecification(PaymentProviderKeys.SafeHaven),
                cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven payment provider is not active.");

        var paymentProviderService = paymentProviderServices.SingleOrDefault(service =>
                string.Equals(service.ProviderKey, paymentProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new PaymentProviderResolutionException(
                $"No payment provider service is registered for '{paymentProvider.ProviderKey}'.");

        var validationResult = await paymentProviderService.ValidateWebhookAsync(
            new PaymentProviderWebhookValidationRequest(command.RawPayload),
            cancellationToken);
        var webhookDetails = CreateWebhookDetails(command.Webhook);

        if (!string.IsNullOrWhiteSpace(webhookDetails.WebhookEventId))
        {
            var existingWebhook = await paymentWebhookInboxRepository.FirstOrDefaultAsync(
                new PaymentWebhookInboxByEventIdSpecification(paymentProvider.Id, webhookDetails.WebhookEventId),
                cancellationToken);
            if (existingWebhook is not null)
            {
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

        if (validationResult.SignatureValidationStatus == SignatureValidationStatus.Invalid)
        {
            inbox.MarkIgnored("invalid_signature", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.InvalidSignature);
        }

        var paymentTransaction = await ResolvePaymentTransactionAsync(webhookDetails, cancellationToken);
        if (paymentTransaction is null || !webhookDetails.IsSupportedEvent)
        {
            inbox.MarkIgnored("transaction_not_found_or_unmapped_status", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.UnidentifiedTransaction);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProcessPaymentWebhookResult(WebhookReceiptStatus.Persisted);
    }

    private static SafeHavenWebhookDetails CreateWebhookDetails<TData>(SafeHavenWebhook<TData> webhook)
    {
        var eventName = string.IsNullOrWhiteSpace(webhook.Event) ? "safehaven.webhook" : webhook.Event.Trim();

        return webhook.Data switch
        {
            SafeHavenAccountCreditWebhookData data => new SafeHavenWebhookDetails(
                NormalizeOptional(data.PaymentReference),
                NormalizeOptional(data.Id),
                eventName,
                CreateWebhookEventId(data.PaymentReference, eventName),
                false),
            SafeHavenAccountDebitWebhookData data => new SafeHavenWebhookDetails(
                NormalizeOptional(data.PaymentReference),
                NormalizeOptional(data.Id),
                eventName,
                CreateWebhookEventId(data.PaymentReference, eventName),
                false),
            SafeHavenVirtualAccountTransferWebhookData data => new SafeHavenWebhookDetails(
                NormalizeOptional(data.PaymentReference),
                NormalizeOptional(data.Id),
                eventName,
                CreateWebhookEventId(data.PaymentReference, eventName),
                true),
            _ => new SafeHavenWebhookDetails(null, null, eventName, null, false)
        };
    }

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

    private static string? CreateWebhookEventId(string? merchantReference, string eventName) =>
        string.IsNullOrWhiteSpace(merchantReference) ? null : $"{merchantReference.Trim()}:{eventName}";

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record SafeHavenWebhookDetails(
        string? MerchantReference,
        string? ProviderReference,
        string WebhookEventName,
        string? WebhookEventId,
        bool IsSupportedEvent);
}
