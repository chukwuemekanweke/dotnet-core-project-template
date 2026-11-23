using BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionPurchaseStatus;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.GetSubscriptionPurchaseStatus;

public sealed class When_GettingPurchaseStatus_WithLifecycleStates_Should
{
    [Theory]
    [InlineData(PaymentStatus.Initiated, false, false, SubscriptionStatus.Pending, "PaymentPending", true)]
    [InlineData(PaymentStatus.Succeeded, false, false, SubscriptionStatus.Pending, "PaymentSucceededFulfillmentPending", true)]
    [InlineData(PaymentStatus.Succeeded, true, false, SubscriptionStatus.Active, "SubscriptionActive", false)]
    [InlineData(PaymentStatus.Failed, false, false, SubscriptionStatus.Pending, "PaymentFailed", false)]
    [InlineData(PaymentStatus.Expired, false, false, SubscriptionStatus.Pending, "PaymentExpired", false)]
    [InlineData(PaymentStatus.Succeeded, false, true, SubscriptionStatus.Pending, "FulfillmentBlocked", false)]
    public async Task ReturnNormalizedStatus(
        PaymentStatus paymentStatus,
        bool fulfilled,
        bool blocked,
        SubscriptionStatus subscriptionStatus,
        string expectedStatus,
        bool shouldContinuePolling)
    {
        var tenantId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var readModelRepository = Substitute.For<ICustomerSubscriptionReadModelRepository>();
        readModelRepository.GetPurchaseStatusAsync(
                Arg.Any<CustomerSubscriptionPurchaseStatusRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateReadModel(paymentStatus, fulfilled, blocked, subscriptionStatus));
        var handler = new GetSubscriptionPurchaseStatusHandler(readModelRepository);

        var response = await handler.HandleAsync(
            new GetSubscriptionPurchaseStatusCommand(
                Guid.CreateVersion7(),
                BillingAccountOwnerType.Stakeholder,
                new ActorContext(stakeholderId, tenantId, "correlation", "flow")),
            CancellationToken.None);

        response.ShouldNotBeNull();
        response.Status.ShouldBe(expectedStatus);
        response.ShouldContinuePolling.ShouldBe(shouldContinuePolling);
        response.IsTerminal.ShouldBe(!shouldContinuePolling);
    }

    private static CustomerSubscriptionPurchaseStatusReadModel CreateReadModel(
        PaymentStatus paymentStatus,
        bool fulfilled,
        bool blocked,
        SubscriptionStatus subscriptionStatus)
    {
        var now = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        return new CustomerSubscriptionPurchaseStatusReadModel(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "pay_reference",
            paymentStatus,
            PaymentMethodType.PaymentLink,
            "Provider",
            10000m,
            "NGN",
            now.AddHours(1),
            paymentStatus == PaymentStatus.Succeeded ? now : null,
            fulfilled ? now : null,
            blocked ? now : null,
            blocked ? "conflicting_entitlement" : null,
            subscriptionStatus,
            fulfilled ? now.AddMonths(1) : null,
            now,
            now);
    }
}
