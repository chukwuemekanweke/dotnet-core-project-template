using BackendProjectTemplate.Contracts.Payments;

namespace BackendProjectTemplate.Contracts.Events;

/// <summary>
/// A payment reached a terminal state other than success (failed, expired, or an underpaid payment that is not
/// credited). Its status will never change again.
/// </summary>
public sealed record UnsuccessfulPaymentConcluded : BaseEvent
{
    public Guid PaymentTransactionId { get; init; }
    public string MerchantReference { get; init; } = string.Empty;
    public PaymentIntent PaymentIntent { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
}
