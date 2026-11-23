using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ReconcilePayments;

public sealed class When_ReconcilingPayments_WithUnderpaidVerification_Should
{
    [Theory]
    [InlineData(PaymentIntent.WalletTopUp, 1)]
    [InlineData(PaymentIntent.Subscription, 0)]
    public async Task MarkTransactionUnderpaidAndCreditOnlyWalletTopUps(PaymentIntent paymentIntent, int expectedPublications)
    {
        var context = new PaymentsFlowTestContext();
        var currency = context.CreateCurrency("NGN");
        var provider = context.CreatePaymentProvider("Bachs", PaymentProviderKeys.Bachs);
        var paymentProviderService = Substitute.For<IPaymentProviderService>();
        var transaction = PaymentTransaction.Create("merchant-underpaid", paymentIntent, provider.Id, 2500m, currency.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        transaction.MarkInitiated("chk_123", null, null, KnownPaymentTransactionChangeReasons.PaymentInitiated);
        transaction.RecordStatusCheck(context.Clock.GetUtcNow().AddHours(-25));

        context.PaymentTransactionRepository.ListAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns([transaction]);
        context.CurrencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>())
            .Returns(currency);
        context.PaymentProviderRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(provider);
        paymentProviderService.ProviderKey.Returns(PaymentProviderKeys.Bachs);
        paymentProviderService.VerifyPaymentAsync(Arg.Any<PaymentProviderVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Underpaid,
                "chk_123",
                "provider_reported_underpayment",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedUnderpayment,
                1000m));
        context.PaymentProviderServices.Add(paymentProviderService);

        await context.CreatePaymentReconciliationService().HandleAsync(
            context.Clock.GetUtcNow().AddHours(-48),
            context.Clock.GetUtcNow().AddMinutes(-10),
            context.Clock.GetUtcNow().AddMinutes(-2),
            50,
            CancellationToken.None);

        transaction.PaymentStatus.ShouldBe(PaymentStatus.Underpaid);
        transaction.AmountPaid.ShouldBe(1000m);
        transaction.StatusChangeReason.ShouldBe(KnownPaymentTransactionChangeReasons.ReconciliationConfirmedUnderpayment);
        await context.EventPublisher.Received(expectedPublications).PublishAsync(
            Arg.Is<SuccessfulPaymentConfirmed>(message => message.Amount == 1000m),
            Arg.Any<CancellationToken>());
    }
}
