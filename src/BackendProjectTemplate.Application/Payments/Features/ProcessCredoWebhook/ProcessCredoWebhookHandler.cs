using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;

namespace BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;

public sealed class ProcessCredoWebhookHandler(
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentWebhookInbox> paymentWebhookInboxRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    ICredoWebhookSignatureValidator credoWebhookSignatureValidator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProcessPaymentWebhookResult> HandleAsync(
        ProcessCredoWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByKeySpecification(PaymentProviderKeys.Credo),
                cancellationToken)
            ?? throw new InvalidOperationException("Credo payment provider is not active.");

        var validationResult = await credoWebhookSignatureValidator.ValidateAsync(
            new PaymentProviderWebhookValidationRequest(command.RawPayload)
            {
                SignatureHeader = command.SignatureHeader
            },
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

    private static CredoWebhookDetails CreateWebhookDetails(CredoWebhook webhook)
    {
        var merchantReference = NormalizeOptional(webhook.Data.BusinessRef);
        var providerReference = NormalizeOptional(webhook.Data.TransRef);
        var eventName = GetRequiredEventName(webhook.Event);
        var webhookEventId = !string.IsNullOrWhiteSpace(merchantReference)
            ? $"{merchantReference}:{eventName}"
            : null;

        return new CredoWebhookDetails(
            merchantReference,
            providerReference,
            eventName,
            webhookEventId,
            eventName is CredoWebhookEvents.TransactionSuccessful or CredoWebhookEvents.TransactionFailed or CredoWebhookEvents.TransactionTransferReverse);
    }

    private async Task<PaymentTransaction?> ResolvePaymentTransactionAsync(
        CredoWebhookDetails webhookDetails,
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetRequiredEventName(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("Credo webhook event name is required.")
            : value.Trim();

    private sealed record CredoWebhookDetails(
        string? MerchantReference,
        string? ProviderReference,
        string WebhookEventName,
        string? WebhookEventId,
        bool IsSupportedEvent);
}
