using BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessPaymentWebhook;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.ProcessBachsWebhook;

public sealed class When_ProcessingBachsPaymentMethodSaved_Should
{
    [Fact]
    public async Task ActivateProtectedAuthorizationIdempotently()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Bachs", PaymentProviderKeys.Bachs);
        var authorization = RecurringPaymentAuthorization.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            provider.Id,
            "cust_123",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            context.Clock.GetUtcNow(),
            context.Clock.GetUtcNow());
        context.PaymentProviderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentProvider>>(),
                Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentWebhookInbox>>(),
                Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.RecurringPaymentAuthorizationRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<RecurringPaymentAuthorization>>(),
                Arg.Any<CancellationToken>())
            .Returns(authorization);
        context.BachsWebhookSignatureValidator.ValidateAsync(
                Arg.Any<BachsWebhookSignatureValidationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Valid, "signature_verified"));
        context.RecurringPaymentAuthorizationProtector.Protect("pm_123").Returns("protected-pm-123");

        var result = await context.CreateBachsWebhookHandler().HandleAsync(
            new ProcessBachsWebhookCommand(
                new BachsWebhook(
                    "evt_saved",
                    BachsWebhookEvents.PaymentMethodSaved,
                    context.Clock.GetUtcNow(),
                    "acct_123",
                    new BachsWebhookData(null, null, null, null, null, "NGN", "pm_123", "cust_123")),
                "{}",
                System.Text.Encoding.UTF8.GetBytes("{}"),
                "signature",
                null,
                null),
            CancellationToken.None);

        result.Status.ShouldBe(WebhookReceiptStatus.Persisted);
        authorization.Status.ShouldBe(RecurringPaymentAuthorizationStatus.Active);
        authorization.AuthorizationReference.ShouldBe("protected-pm-123");
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
