using BackendProjectTemplate.Domain.Payments.ReadModels;

namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentDetail;

public sealed class GetAdminPaymentDetailHandler(IPaymentAdministrationReadModelRepository readModelRepository)
{
    public const int DefaultWebhookHistoryLimit = 20;
    public const int MaxWebhookHistoryLimit = 100;

    public async Task<GetAdminPaymentDetailResponse?> HandleAsync(
        GetAdminPaymentDetailCommand command,
        CancellationToken cancellationToken)
    {
        var historyLimit = Math.Clamp(command.WebhookHistoryLimit, 1, MaxWebhookHistoryLimit);
        var detail = await readModelRepository.GetPaymentDetailAsync(
            command.PaymentTransactionId,
            historyLimit,
            cancellationToken);
        if (detail is null)
        {
            return null;
        }

        var payment = detail.Payment;
        var statusReason = NormalizeDiagnosticCode(payment.StatusChangeReason);
        var failureReason = string.IsNullOrWhiteSpace(payment.FailureReason) ? null : "Payment provider reported a failure.";
        return new GetAdminPaymentDetailResponse(
            payment.TransactionId,
            payment.MerchantReference,
            payment.ProviderReference,
            payment.TenantId,
            payment.StakeholderId,
            payment.StakeholderDisplayName,
            payment.InitiatedByUserId,
            payment.PaymentProviderId,
            payment.PaymentProviderName,
            payment.PaymentProviderKey,
            payment.Amount,
            payment.CurrencyId,
            payment.CurrencyCode,
            payment.PaymentIntent.ToString(),
            payment.PaymentMethodType.ToString(),
            payment.PaymentStatus.ToString(),
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc,
            payment.CompletedAtUtc,
            payment.FailedAtUtc,
            payment.ExpiresAtUtc,
            payment.LastStatusCheckAtUtc,
            statusReason,
            failureReason,
            new AdminPaymentCurrentStatusResponse(payment.PaymentStatus.ToString(), statusReason, payment.UpdatedAtUtc),
            detail.Webhooks.Select(webhook => new AdminPaymentWebhookResponse(
                webhook.WebhookId,
                webhook.WebhookEventName,
                webhook.WebhookEventId,
                webhook.WebhookProcessingStatus.ToString(),
                webhook.SignatureValidationStatus.ToString(),
                NormalizeDiagnosticCode(webhook.StatusChangeReason),
                webhook.ReceivedAtUtc,
                webhook.ProcessedAtUtc)).ToArray(),
            new AdminPaymentReconciliationResponse(payment.LastStatusCheckAtUtc),
            new AdminPaymentFulfillmentResponse(
                detail.Fulfillment.Type,
                detail.Fulfillment.Status,
                detail.Fulfillment.ReferenceId,
                detail.Fulfillment.FulfilledAtUtc,
                detail.Fulfillment.Reason));
    }

    private static string? NormalizeDiagnosticCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100)
        {
            return null;
        }

        return value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.')
            ? value
            : null;
    }
}
