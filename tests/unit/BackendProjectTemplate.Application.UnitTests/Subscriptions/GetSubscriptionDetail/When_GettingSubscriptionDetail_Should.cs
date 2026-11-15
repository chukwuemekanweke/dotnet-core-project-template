using BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.GetSubscriptionDetail;

public sealed class When_GettingSubscriptionDetail_Should
{
    [Fact]
    public async Task MapFinancialAndEntitlementHistoryWithBoundedLimit()
    {
        var repository = Substitute.For<ISubscriptionAdministrationReadModelRepository>();
        var subscriptionId = Guid.CreateVersion7();
        var now = AdminSubscriptionTestContext.Now;
        var summary = new AdminSubscriptionListItemReadModel(
            subscriptionId, Guid.CreateVersion7(), "Tenant", Guid.CreateVersion7(), BillingAccountOwnerType.Tenant, null, "Tenant",
            Guid.CreateVersion7(), "GOLD", "Gold", SubscriptionStatus.Active, now, now.AddMonths(1), now, null, now.AddMonths(1),
            true, false, now, SubscriptionOrigin.Purchase, null);
        var renewalId = Guid.CreateVersion7();
        repository.GetSubscriptionDetailAsync(subscriptionId, GetSubscriptionDetailHandler.MaxHistoryLimit, Arg.Any<CancellationToken>())
            .Returns(new AdminSubscriptionDetailReadModel(
                summary, null, null, null, null, Guid.CreateVersion7(), null,
                new AdminSubscriptionPlanReadModel(summary.PlanId, "GOLD", "Gold", 5000m, Guid.CreateVersion7(), "NGN", BillingInterval.Monthly, true, now, now),
                new AdminSubscriptionPurchaseReadModel(Guid.CreateVersion7(), Guid.CreateVersion7(), 5000m, Guid.CreateVersion7(), Guid.CreateVersion7(), now, now, null, null),
                [new AdminSubscriptionPeriodReadModel(Guid.CreateVersion7(), now, now.AddMonths(1), "GOLD", 5000m, Guid.CreateVersion7(), BillingInterval.Monthly, SubscriptionActivationSource.PaidPurchase, Guid.CreateVersion7(), Guid.CreateVersion7())],
                [new AdminSubscriptionPaymentReadModel(Guid.CreateVersion7(), "pay_1", 5000m, Guid.CreateVersion7(), "NGN", PaymentStatus.Succeeded, renewalId, now, now, null)],
                [new AdminSubscriptionRenewalReadModel(renewalId, now.AddMonths(1), now.AddMonths(2), 5000m, Guid.CreateVersion7(), SubscriptionRenewalStatus.Pending, now, null, null,
                    [new AdminSubscriptionRenewalAttemptReadModel(Guid.CreateVersion7(), 1, Guid.CreateVersion7(), SubscriptionRenewalAttemptStatus.Submitted, null, null, now, null)])],
                [new AdminSubscriptionStatusHistoryReadModel(Guid.CreateVersion7(), SubscriptionStatus.Pending, SubscriptionStatus.Active, "initial_payment_confirmed", null, null, Guid.CreateVersion7(), Guid.CreateVersion7(), now)],
                []));
        var handler = new GetSubscriptionDetailHandler(repository);

        var detail = await handler.HandleAsync(new GetSubscriptionDetailCommand(subscriptionId, 5000), CancellationToken.None);
        var missing = await handler.HandleAsync(new GetSubscriptionDetailCommand(Guid.CreateVersion7(), 10), CancellationToken.None);

        detail.ShouldNotBeNull();
        detail.HistoryLimit.ShouldBe(GetSubscriptionDetailHandler.MaxHistoryLimit);
        detail.Subscription.Origin.ShouldBe("Purchase");
        detail.Plan.CurrencyCode.ShouldBe("NGN");
        detail.Payments.ShouldHaveSingleItem().PaymentStatus.ShouldBe("Succeeded");
        detail.Renewals.ShouldHaveSingleItem().Attempts.ShouldHaveSingleItem().Status.ShouldBe("Submitted");
        detail.StatusHistory.ShouldHaveSingleItem().PreviousStatus.ShouldBe("Pending");
        detail.Periods.ShouldHaveSingleItem().ActivationSource.ShouldBe("PaidPurchase");
        detail.Allocations.ShouldBeEmpty();
        missing.ShouldBeNull();
    }
}
