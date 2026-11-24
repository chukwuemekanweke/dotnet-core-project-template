using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Consumer.Payments;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Payments.UnsuccessfulPaymentConcluded;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingUnsuccessfulPaymentConcluded_WithExpiredInitialPayment_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _paymentTransactionId;
    private Guid _tenantId;
    private Guid _stakeholderId;
    private Guid _currencyId;
    private Guid _billingAccountId;
    private Guid _subscriptionId;
    private Guid _purchaseId;
    private Guid _planId;
    private Guid _stakeholderTypeId;
    private Guid _userId;

    protected override async Task InitializeWorkerTestAsync()
    {
        _paymentTransactionId = Guid.CreateVersion7();
        _tenantId = Guid.CreateVersion7();
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var tenant = Tenant.Create(_tenantId, "Subscription Tenant", $"subscription-{_tenantId:N}");
        var user = AppUser.Create($"subscription-{Guid.CreateVersion7():N}@example.com", "Subscription", "Owner");
        var stakeholderType = StakeholderType.Create(_tenantId, "Individual Customer", "individual-customer");
        var createResult = await identityService.CreateAsync(user);
        createResult.Succeeded.ShouldBeTrue();
        var stakeholder = Stakeholder.Create(
            user.Id,
            _tenantId,
            Guid.CreateVersion7(),
            stakeholderType.Id,
            "Subscription",
            "Owner");
        var currency = Currency.Create("TST", "Test Currency", true);
        var billingAccount = BillingAccount.Create(
            _tenantId,
            BillingAccountOwnerType.Stakeholder,
            stakeholder.Id,
            "Subscription Owner");
        var plan = SubscriptionPlan.Create(
            "INTEGRATION_MONTHLY",
            "Integration Monthly",
            5000m,
            currency.Id,
            BillingInterval.Monthly,
            true);
        var payment = PaymentTransaction.Create(
            "merchant-ref",
            PaymentIntent.Subscription,
            Guid.CreateVersion7(),
            plan.Price,
            currency.Id,
            stakeholder.CountryId,
            user.Id,
            stakeholder.Id,
            _tenantId);
        typeof(Domain.Common.Entities.Entity)
            .GetProperty(nameof(Domain.Common.Entities.Entity.Id))!
            .SetValue(payment, _paymentTransactionId);
        payment.MarkInitiated("provider-ref", null, null, "initiated");
        payment.MarkExpired("provider-ref", "checkout_expired", "expired");
        var subscription = Domain.Subscriptions.Entities.Subscription.CreatePending(
            _tenantId,
            billingAccount.Id,
            plan.Id,
            true,
            now,
            Guid.CreateVersion7(),
            stakeholder.Id.ToString());
        var purchase = SubscriptionPurchase.Create(
            _tenantId,
            billingAccount.Id,
            subscription.Id,
            plan,
            payment.Id,
            stakeholder.Id);

        dbContext.Tenants.Add(tenant);
        dbContext.StakeholderTypes.Add(stakeholderType);
        dbContext.Stakeholders.Add(stakeholder);
        dbContext.Currencies.Add(currency);
        dbContext.BillingAccounts.Add(billingAccount);
        dbContext.SubscriptionPlans.Add(plan);
        dbContext.PaymentTransactions.Add(payment);
        dbContext.Subscriptions.Add(subscription);
        dbContext.SubscriptionPurchases.Add(purchase);
        await dbContext.SaveChangesAsync();

        _userId = user.Id;
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
        _currencyId = currency.Id;
        _billingAccountId = billingAccount.Id;
        _subscriptionId = subscription.Id;
        _purchaseId = purchase.Id;
        _planId = plan.Id;
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateDbContextScope();
        await scope.DbContext.SubscriptionPeriods
            .Where(item => item.SubscriptionId == _subscriptionId)
            .ExecuteDeleteAsync();
        await scope.DbContext.SubscriptionStatusHistory
            .Where(item => item.SubscriptionId == _subscriptionId)
            .ExecuteDeleteAsync();
        await scope.DbContext.SubscriptionPurchases
            .Where(item => item.Id == _purchaseId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Subscriptions
            .Where(item => item.Id == _subscriptionId)
            .ExecuteDeleteAsync();
        await scope.DbContext.PaymentTransactions
            .Where(item => item.Id == _paymentTransactionId)
            .ExecuteDeleteAsync();
        await scope.DbContext.SubscriptionPlans
            .Where(item => item.Id == _planId)
            .ExecuteDeleteAsync();
        await scope.DbContext.BillingAccounts
            .Where(item => item.Id == _billingAccountId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Currencies
            .Where(item => item.Id == _currencyId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Stakeholders
            .Where(item => item.Id == _stakeholderId)
            .ExecuteDeleteAsync();
        await scope.DbContext.StakeholderTypes
            .Where(item => item.Id == _stakeholderTypeId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Users
            .Where(item => item.Id == _userId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Tenants
            .Where(item => item.Id == _tenantId)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task ExpireThePendingSubscriptionSoTheAccountCanPurchaseAgain()
    {
        await WhenHandlingTheEventAsync();
        await WhenHandlingTheEventAsync();
        await ThenTheSubscriptionIsExpiredOnce();

        async Task WhenHandlingTheEventAsync()
        {
            using var scope = CreateScope();
            await scope.ServiceProvider.GetRequiredService<UnsuccessfulPaymentConcludedHandler>().HandleAsync(
                new Contracts.Events.UnsuccessfulPaymentConcluded
                {
                    PaymentTransactionId = _paymentTransactionId,
                    MerchantReference = "merchant-ref",
                    PaymentIntent = PaymentIntent.Subscription,
                    PaymentStatus = PaymentStatus.Expired,
                    StakeholderId = _stakeholderId,
                    TenantId = _tenantId,
                    FlowId = "flow-123"
                },
                CancellationToken.None);
        }

        async Task ThenTheSubscriptionIsExpiredOnce()
        {
            using var scope = CreateDbContextScope();
            var subscription = await scope.DbContext.Subscriptions.SingleAsync(item => item.Id == _subscriptionId);
            subscription.Status.ShouldBe(SubscriptionStatus.Expired);
            subscription.EndedAtUtc.ShouldNotBeNull();
            var history = await scope.DbContext.SubscriptionStatusHistory
                .Where(item => item.SubscriptionId == _subscriptionId &&
                    item.Reason == SubscriptionLifecycleReasons.InitialPaymentUnsuccessful)
                .ToListAsync();
            history.Count.ShouldBe(1);
            history[0].PaymentTransactionId.ShouldBe(_paymentTransactionId);
            (await scope.DbContext.SubscriptionPeriods.AnyAsync(item => item.SubscriptionId == _subscriptionId)).ShouldBeFalse();
        }
    }
}
