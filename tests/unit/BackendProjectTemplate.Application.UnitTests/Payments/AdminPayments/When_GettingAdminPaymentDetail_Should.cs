using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentDetail;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Payments.AdminPayments;

public sealed class When_GettingAdminPaymentDetail_Should
{
    [Fact]
    public async Task BoundHistoryAndRemoveUnsafeProviderDiagnostics()
    {
        var repository = Substitute.For<IPaymentAdministrationReadModelRepository>();
        var id = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
        repository.GetPaymentDetailAsync(id, GetAdminPaymentDetailHandler.MaxWebhookHistoryLimit, Arg.Any<CancellationToken>())
            .Returns(new AdminPaymentDetailReadModel(
                new AdminPaymentDetailCoreReadModel(
                    id, "merchant-1", "provider-1", Guid.CreateVersion7(), Guid.CreateVersion7(), "Ada Lovelace",
                    Guid.CreateVersion7(), Guid.CreateVersion7(), "Bachs", "bachs", 125000m, Guid.CreateVersion7(), "NGN",
                    PaymentIntent.Subscription, PaymentMethodType.Card, PaymentStatus.Failed, now, now.AddMinutes(1), null,
                    now.AddMinutes(1), now.AddHours(1), now.AddSeconds(30), "unsafe diagnostic with spaces", "secret provider detail"),
                [new AdminPaymentWebhookReadModel(
                    Guid.CreateVersion7(), "checkout.failed", "evt-1", WebhookProcessingStatus.Failed,
                    SignatureValidationStatus.Valid, "signature_verified", now, now.AddSeconds(1))],
                new AdminPaymentFulfillmentReadModel("SubscriptionPurchase", "Blocked", Guid.CreateVersion7(), null,
                    "Subscription purchase fulfillment is blocked.")));

        var result = await new GetAdminPaymentDetailHandler(repository).HandleAsync(
            new GetAdminPaymentDetailCommand(id, 1000), CancellationToken.None);

        result.ShouldNotBeNull();
        result.StatusChangeReason.ShouldBeNull();
        result.FailureReason.ShouldBe("Payment provider reported a failure.");
        result.Webhooks.ShouldHaveSingleItem().StatusChangeReason.ShouldBe("signature_verified");
        result.Reconciliation.LastStatusCheckAtUtc.ShouldBe(now.AddSeconds(30));
        result.Fulfillment.Status.ShouldBe("Blocked");
        await repository.Received(1).GetPaymentDetailAsync(
            id,
            GetAdminPaymentDetailHandler.MaxWebhookHistoryLimit,
            Arg.Any<CancellationToken>());
    }
}
