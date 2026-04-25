using BackendProjectTemplate.Application.Payments.Features.ReconcilePayments;
using BackendProjectTemplate.Application.UnitTests.Payments;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ReconcilePayments;

public sealed class When_ReconcilingPayments_WithSuccessfulVerification_Should
{
    [Fact]
    public async Task MarkTransactionSucceeded()
    {
        var context = new PaymentsFlowTestContext();
        var currency = context.CreateCurrency("NGN");
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        var paymentProviderService = Substitute.For<IPaymentProviderService>();
        var transaction = PaymentTransaction.Create(
            "merchant-success",
            PaymentIntent.WalletTopUp,
            provider.Id,
            1000m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            context.Clock.GetUtcNow());

        transaction.MarkInitiated("provider-ref", null, null, KnownPaymentTransactionChangeReasons.PaymentInitiated);

        context.PaymentTransactionRepository.ListAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns([transaction]);
        context.CurrencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>())
            .Returns(currency);
        context.PaymentProviderRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(provider);
        paymentProviderService.ProviderKey.Returns(PaymentProviderKeys.Credo);
        paymentProviderService.VerifyPaymentAsync(Arg.Any<PaymentProviderVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Succeeded,
                "provider-ref",
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess,
                new Dictionary<string, string> { ["provider"] = "credo" }));
        context.PaymentProviderServices.Add(paymentProviderService);

        var result = await context.CreatePaymentReconciliationService().HandleAsync(
            context.Clock.GetUtcNow().AddMinutes(-10),
            context.Clock.GetUtcNow().AddMinutes(-2),
            50,
            CancellationToken.None);

        result.ProcessedCount.ShouldBe(1);
        transaction.PaymentStatus.ShouldBe(PaymentStatus.Succeeded);
        transaction.ProviderPayloadMetadata["provider"].ShouldBe("credo");
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<SuccessfulPaymentConfirmed>(message =>
                message.PaymentTransactionId == transaction.Id &&
                message.PaymentIntent == PaymentIntent.WalletTopUp &&
                message.PaymentProviderId == provider.Id),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
