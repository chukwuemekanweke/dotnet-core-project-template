using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Payments.ActivateSubscription;

public sealed class When_HandlingActivateSubscription_WithConcurrentEntitlementCommit_Should
{
    [Fact]
    public async Task DiscardTheActivationAndPersistTheBlockedPurchaseInTheSameDelivery()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var currencyId = Guid.CreateVersion7();
        var command = context.CreateActivateSubscriptionCommand(5000m, currencyId);
        var seeded = context.SeededPurchase!;
        // After DiscardChanges the real handler reloads an untouched purchase from the database.
        var reloaded = SubscriptionPurchase.Create(
            seeded.TenantId,
            seeded.BillingAccountId,
            seeded.SubscriptionId,
            SubscriptionPlan.Create(seeded.PlanCode, "Reloaded", seeded.ExpectedAmount, currencyId, seeded.BillingInterval, true),
            SubscriptionPlan.Create(seeded.PlanCode, "Reloaded", seeded.ExpectedAmount, currencyId, seeded.BillingInterval, true).Prices.Single().BillingInterval,
            seeded.PaymentTransactionId,
            seeded.InitiatedByStakeholderId);
        context.SubscriptionPurchaseRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPurchase>>(), Arg.Any<CancellationToken>())
            .Returns(seeded, reloaded);
        var saves = 0;
        context.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            saves++;
            return saves == 1
                ? throw new SubscriptionEntitlementConflictException("overlap")
                : Task.FromResult(1);
        });

        await context.CreateActivateSubscriptionHandler().HandleAsync(command, CancellationToken.None);

        context.UnitOfWork.Received(1).DiscardChanges();
        saves.ShouldBe(2);
        reloaded.IsFulfillmentBlocked.ShouldBeTrue();
        reloaded.FulfillmentBlockedReason.ShouldBe(SubscriptionFulfillmentBlockReasons.EntitlementConflict);
        await context.MessageInboxRepository.Received(1).AddAsync(Arg.Any<Domain.Common.Messaging.MessageInbox>(), Arg.Any<CancellationToken>());
    }
}
