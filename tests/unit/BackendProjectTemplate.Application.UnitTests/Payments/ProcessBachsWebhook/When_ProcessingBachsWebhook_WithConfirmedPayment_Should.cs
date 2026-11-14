using BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ProcessBachsWebhook;

public sealed class When_ProcessingBachsWebhook_WithConfirmedPayment_Should
{
    [Fact]
    public async Task PublishSuccessfulPaymentThroughExistingPipeline()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Bachs", PaymentProviderKeys.Bachs);
        var currency = context.CreateCurrency("NGN");
        var transaction = PaymentTransaction.Create(
            "pay_merchant_reference",
            PaymentIntent.WalletTopUp,
            provider.Id,
            2500m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated("chk_123", null, null, "payment_initiated");
        PaymentWebhookInbox? capturedInbox = null;
        var providerService = Substitute.For<IPaymentProviderService>();
        providerService.ProviderKey.Returns(PaymentProviderKeys.Bachs);
        providerService.VerifyPaymentAsync(Arg.Any<PaymentProviderVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Succeeded,
                "chk_123",
                null,
                "reconciliation_confirmed_success"));
        context.PaymentProviderServices.Add(providerService);
        context.PaymentProviderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentProvider>>(),
                Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentWebhookInbox>>(),
                Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.PaymentWebhookInboxRepository.AddAsync(
                Arg.Do<PaymentWebhookInbox>(inbox => capturedInbox = inbox),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
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

        var result = await context.CreateBachsWebhookHandler().HandleAsync(
            new ProcessBachsWebhookCommand(
                new BachsWebhook(
                    "evt_123",
                    BachsWebhookEvents.CollectionSucceeded,
                    context.Clock.GetUtcNow(),
                    "acct_123",
                    new BachsWebhookData(
                        "ch_123",
                        "chk_123",
                        transaction.MerchantReference,
                        "SUCCEEDED",
                        "25.00",
                        "NGN")),
                "{\"id\":\"evt_123\"}",
                System.Text.Encoding.UTF8.GetBytes("{\"id\":\"evt_123\"}"),
                "t=1,v1=signature",
                null,
                null),
            CancellationToken.None);

        result.Status.ShouldBe(WebhookReceiptStatus.Persisted);
        transaction.PaymentStatus.ShouldBe(PaymentStatus.Succeeded);
        capturedInbox.ShouldNotBeNull();
        capturedInbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<SuccessfulPaymentConfirmed>(message =>
                message.PaymentTransactionId == transaction.Id &&
                message.PaymentIntent == PaymentIntent.WalletTopUp),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
