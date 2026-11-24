using BackendProjectTemplate.Application.Subscriptions.Features.RetrySubscriptionPurchaseFulfillment;
using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.RetrySubscriptionPurchaseFulfillment;

public sealed class When_RetryingBlockedPurchaseFulfillment_Should
{
    [Fact]
    public async Task ReEnqueueActivationOnlyForBlockedVerifiedPayments()
    {
        var tenantId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var payment = PaymentTransaction.Create(
            "pay_retry", PaymentIntent.Subscription, Guid.CreateVersion7(), 5000m, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), stakeholderId, tenantId);
        payment.MarkInitiated("ref", null, null, "initiated");
        payment.MarkSucceeded("ref", "ok", AdminSubscriptionTestContext.Now);
        var plan = SubscriptionPlan.Create("RETRY", "Retry", 5000m, payment.CurrencyId, BillingInterval.Monthly, true);
        var purchase = SubscriptionPurchase.Create(tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), plan, plan.Prices.Single().BillingInterval, payment.Id, stakeholderId);
        var purchaseRepository = Substitute.For<IRepository<SubscriptionPurchase>>();
        var paymentRepository = Substitute.For<IRepository<PaymentTransaction>>();
        var commandSender = Substitute.For<ICommandSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        purchaseRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPurchase>>(), Arg.Any<CancellationToken>()).Returns(purchase);
        paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        var handler = new RetrySubscriptionPurchaseFulfillmentHandler(
            purchaseRepository, paymentRepository, commandSender, Substitute.For<ICustomTelemetryContext>(), unitOfWork);
        var command = new RetrySubscriptionPurchaseFulfillmentCommand(purchase.SubscriptionId, tenantId, new ActorContext(Guid.CreateVersion7(), null, "c", "f"));

        var notBlocked = await handler.HandleAsync(command, CancellationToken.None);
        purchase.MarkFulfillmentBlocked(SubscriptionFulfillmentBlockReasons.EntitlementConflict, AdminSubscriptionTestContext.Now);
        var wrongTenant = await handler.HandleAsync(command with { TenantId = Guid.CreateVersion7() }, CancellationToken.None);
        var requested = await handler.HandleAsync(command, CancellationToken.None);

        notBlocked.Status.ShouldBe(RetrySubscriptionPurchaseFulfillmentStatus.NotBlocked);
        wrongTenant.Status.ShouldBe(RetrySubscriptionPurchaseFulfillmentStatus.NotFound);
        requested.Status.ShouldBe(RetrySubscriptionPurchaseFulfillmentStatus.Requested);
        await commandSender.Received(1).SendAsync(
            Arg.Is<ActivateSubscriptionCommand>(activation =>
                activation.PaymentTransactionId == payment.Id &&
                activation.TenantId == tenantId &&
                activation.Amount == payment.Amount &&
                activation.MerchantReference == payment.MerchantReference),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
