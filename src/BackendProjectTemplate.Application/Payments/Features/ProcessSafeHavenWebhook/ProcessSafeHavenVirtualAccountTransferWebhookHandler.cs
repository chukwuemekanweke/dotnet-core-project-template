using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;

namespace BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook;

public sealed class ProcessSafeHavenVirtualAccountTransferWebhookHandler(
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentWebhookInbox> paymentWebhookInboxRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ProcessSafeHavenWebhookHandlerBase<SafeHavenVirtualAccountTransferWebhookData>(
        paymentProviderRepository,
        paymentWebhookInboxRepository,
        paymentTransactionRepository,
        paymentProviderServices,
        unitOfWork,
        timeProvider)
{
    protected override SafeHavenWebhookDetails CreateWebhookDetails(SafeHavenWebhook<SafeHavenVirtualAccountTransferWebhookData> webhook)
    {
        var eventName = GetRequiredEventName(webhook.Event);

        return new SafeHavenWebhookDetails(
            NormalizeOptional(webhook.Data.PaymentReference),
            NormalizeOptional(webhook.Data.Id),
            eventName,
            CreateWebhookEventId(webhook.Data.PaymentReference, eventName),
            true);
    }
}
