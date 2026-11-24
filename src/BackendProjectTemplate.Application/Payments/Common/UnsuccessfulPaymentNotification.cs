using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Application.Payments.Common;

public static class UnsuccessfulPaymentNotification
{
    // Only subscription payments have a downstream state (the Pending subscription) that must react to an
    // unsuccessful outcome; wallet top-ups need nothing.
    public static Task PublishAsync(
        PaymentTransaction transaction,
        IEventPublisher eventPublisher,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (transaction.PaymentIntent != PaymentIntent.Subscription ||
            !transaction.HasReachedTerminalState() ||
            transaction.PaymentStatus == PaymentStatus.Succeeded)
        {
            return Task.CompletedTask;
        }

        return eventPublisher.PublishAsync(
            new UnsuccessfulPaymentConcluded
            {
                PaymentTransactionId = transaction.Id,
                MerchantReference = transaction.MerchantReference,
                PaymentIntent = transaction.PaymentIntent,
                PaymentStatus = transaction.PaymentStatus,
                StakeholderId = transaction.StakeholderId,
                TenantId = transaction.TenantId,
                FlowId = string.Empty,
                OccuredAt = now
            },
            cancellationToken);
    }
}
