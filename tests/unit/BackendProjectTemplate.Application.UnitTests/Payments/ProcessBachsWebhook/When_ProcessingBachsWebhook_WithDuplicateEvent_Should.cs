using BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ProcessBachsWebhook;

public sealed class When_ProcessingBachsWebhook_WithDuplicateEvent_Should
{
    [Fact]
    public async Task AvoidPublishingAnotherSuccessEvent()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Bachs", PaymentProviderKeys.Bachs);
        var existingInbox = PaymentWebhookInbox.Create(
            provider.Id,
            "pay_merchant_reference",
            "chk_123",
            BachsWebhookEvents.CollectionSucceeded,
            "evt_123",
            "{}",
            SignatureValidationStatus.Valid,
            null,
            context.Clock.GetUtcNow());
        context.PaymentProviderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentProvider>>(),
                Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentWebhookInbox>>(),
                Arg.Any<CancellationToken>())
            .Returns(existingInbox);
        context.BachsWebhookSignatureValidator.ValidateAsync(
                Arg.Any<BachsWebhookSignatureValidationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Valid, null));

        var result = await context.CreateBachsWebhookHandler().HandleAsync(
            new ProcessBachsWebhookCommand(
                new BachsWebhook(
                    "evt_123",
                    BachsWebhookEvents.CollectionSucceeded,
                    context.Clock.GetUtcNow(),
                    "acct_123",
                    new BachsWebhookData(null, "chk_123", "pay_merchant_reference", "SUCCEEDED", "25.00", "NGN")),
                "{}",
                System.Text.Encoding.UTF8.GetBytes("{}"),
                "t=1,v1=signature",
                null,
                null),
            CancellationToken.None);

        result.Status.ShouldBe(WebhookReceiptStatus.Duplicate);
        await context.EventPublisher.DidNotReceiveWithAnyArgs().PublishAsync<SuccessfulPaymentConfirmed>(default!, default);
        await context.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
