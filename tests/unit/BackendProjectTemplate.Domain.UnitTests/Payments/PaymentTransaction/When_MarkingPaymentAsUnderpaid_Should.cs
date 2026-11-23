using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Payments.PaymentTransactions;

public sealed class When_MarkingPaymentAsUnderpaid_Should
{
    [Fact]
    public void RecordCollectedAmountAsTerminalStatus()
    {
        var transaction = CreateInitiatedTransaction();
        var closedAt = DateTimeOffset.UtcNow;

        transaction.MarkUnderpaid("chk_123", 400m, "reconciliation_confirmed_underpayment", closedAt);

        transaction.PaymentStatus.ShouldBe(PaymentStatus.Underpaid);
        transaction.AmountPaid.ShouldBe(400m);
        transaction.Amount.ShouldBe(1000m);
        transaction.CompletedAtUtc.ShouldBe(closedAt);
        transaction.HasReachedTerminalState().ShouldBeTrue();
        Should.Throw<AggregateStateException>(() =>
            transaction.MarkSucceeded("chk_123", "success", DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    [InlineData(1500)]
    public void RejectAmountThatIsNotAPartialPayment(decimal amountPaid)
    {
        var transaction = CreateInitiatedTransaction();

        Should.Throw<AggregateStateException>(() =>
            transaction.MarkUnderpaid("chk_123", amountPaid, "reconciliation_confirmed_underpayment", DateTimeOffset.UtcNow));
        transaction.PaymentStatus.ShouldBe(PaymentStatus.Initiated);
    }

    private static PaymentTransaction CreateInitiatedTransaction()
    {
        var transaction = PaymentTransaction.Create("merchant-ref", PaymentIntent.Subscription, Guid.CreateVersion7(), 1000m, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        transaction.MarkInitiated("chk_123", null, null, "payment_initiated");
        return transaction;
    }
}
