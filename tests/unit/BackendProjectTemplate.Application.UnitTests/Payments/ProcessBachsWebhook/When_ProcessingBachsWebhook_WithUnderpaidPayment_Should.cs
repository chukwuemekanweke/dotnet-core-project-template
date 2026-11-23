using BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ProcessBachsWebhook;

public sealed class When_ProcessingBachsWebhook_WithUnderpaidPayment_Should
{
    [Theory]
    [InlineData(BachsWebhookEvents.CollectionUnderpaid)]
    [InlineData(BachsWebhookEvents.CheckoutExpired)]
    public async Task CreditCollectedAmount_ForWalletTopUp(string eventName)
    {
        var (context, transaction) = Arrange(PaymentIntent.WalletTopUp, Underpaid());

        var result = await HandleAsync(context, transaction, eventName);

        result.Status.ShouldBe(WebhookReceiptStatus.Persisted);
        transaction.PaymentStatus.ShouldBe(PaymentStatus.Underpaid);
        transaction.AmountPaid.ShouldBe(1000m);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<SuccessfulPaymentConfirmed>(message =>
                message.PaymentTransactionId == transaction.Id &&
                message.PaymentIntent == PaymentIntent.WalletTopUp &&
                message.Amount == 1000m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HoldSubscriptionForReviewWithoutFulfilment()
    {
        var (context, transaction) = Arrange(PaymentIntent.Subscription, Underpaid());

        var result = await HandleAsync(context, transaction, BachsWebhookEvents.CheckoutExpired);

        result.Status.ShouldBe(WebhookReceiptStatus.Persisted);
        transaction.PaymentStatus.ShouldBe(PaymentStatus.Underpaid);
        transaction.AmountPaid.ShouldBe(1000m);
        await context.EventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<SuccessfulPaymentConfirmed>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(BachsWebhookEvents.CollectionUnderpaid)]
    [InlineData(BachsWebhookEvents.CheckoutCompleted)]
    public async Task ReverifyCheckout_ForNewlySupportedEvents(string eventName)
    {
        var (context, transaction) = Arrange(
            PaymentIntent.WalletTopUp,
            new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Processing,
                "chk_123",
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing));

        var result = await HandleAsync(context, transaction, eventName);

        result.Status.ShouldBe(WebhookReceiptStatus.Persisted);
        transaction.PaymentStatus.ShouldBe(PaymentStatus.Initiated);
        await context.PaymentProviderServices.Single().Received(1).VerifyPaymentAsync(
            Arg.Any<PaymentProviderVerificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static PaymentProviderVerificationResult Underpaid() =>
        new(
            PaymentProviderVerificationStatus.Underpaid,
            "chk_123",
            "provider_reported_underpayment",
            KnownPaymentTransactionChangeReasons.ReconciliationConfirmedUnderpayment,
            1000m);

    private static (PaymentsFlowTestContext Context, PaymentTransaction Transaction) Arrange(
        PaymentIntent paymentIntent,
        PaymentProviderVerificationResult verification)
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Bachs", PaymentProviderKeys.Bachs);
        var currency = context.CreateCurrency("NGN");
        var transaction = PaymentTransaction.Create(
            "pay_merchant_reference",
            paymentIntent,
            provider.Id,
            2500m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated("chk_123", null, null, "payment_initiated");
        var providerService = Substitute.For<IPaymentProviderService>();
        providerService.ProviderKey.Returns(PaymentProviderKeys.Bachs);
        providerService.VerifyPaymentAsync(Arg.Any<PaymentProviderVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(verification);
        context.PaymentProviderServices.Add(providerService);
        context.PaymentProviderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentProvider>>(),
                Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentWebhookInbox>>(),
                Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.PaymentTransactionRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentTransaction>>(),
                Arg.Any<CancellationToken>())
            .Returns(transaction);
        context.CurrencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>())
            .Returns(currency);
        context.BachsWebhookSignatureValidator.ValidateAsync(
                Arg.Any<BachsWebhookSignatureValidationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(
                SignatureValidationStatus.Valid,
                "signature_verified"));
        return (context, transaction);
    }

    private static Task<ProcessPaymentWebhookResult> HandleAsync(
        PaymentsFlowTestContext context,
        PaymentTransaction transaction,
        string eventName) =>
        context.CreateBachsWebhookHandler().HandleAsync(
            new ProcessBachsWebhookCommand(
                new BachsWebhook(
                    "evt_123",
                    eventName,
                    context.Clock.GetUtcNow(),
                    "acct_123",
                    new BachsWebhookData(
                        "ch_123",
                        "chk_123",
                        transaction.MerchantReference,
                        "UNDERPAID",
                        "25.00",
                        "NGN")),
                "{\"id\":\"evt_123\"}",
                System.Text.Encoding.UTF8.GetBytes("{\"id\":\"evt_123\"}"),
                "t=1,v1=signature",
                null,
                null),
            CancellationToken.None);
}
