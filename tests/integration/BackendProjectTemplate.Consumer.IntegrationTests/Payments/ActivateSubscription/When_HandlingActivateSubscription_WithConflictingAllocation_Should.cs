using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Consumer.Payments;
using BackendProjectTemplate.Contracts.Commands.Payments;
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

namespace BackendProjectTemplate.Consumer.IntegrationTests.Payments.ActivateSubscription;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingActivateSubscription_WithConflictingAllocation_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _tenantId;
    private Guid _userId;
    private Guid _stakeholderId;
    private Guid _currencyId;
    private Guid _billingAccountId;
    private Guid _planId;
    private Guid _paidSubscriptionId;
    private Guid _purchaseId;
    private Guid _paymentId;
    private string _merchantReference = string.Empty;
    private SubscriptionPlan _plan = default!;
    private Guid _messageId = Guid.CreateVersion7();

    protected override async Task InitializeWorkerTestAsync()
    {
        _tenantId = Guid.CreateVersion7();
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var tenant = Tenant.Create(_tenantId, "Allocation Tenant", $"allocation-{_tenantId:N}");
        var user = AppUser.Create($"allocation-{Guid.CreateVersion7():N}@example.com", "Allocation", "Owner");
        (await identityService.CreateAsync(user)).Succeeded.ShouldBeTrue();
        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(user.Id, _tenantId, Guid.CreateVersion7(), stakeholderType.Id, "Allocation", "Owner");
        var currency = Currency.Create(Guid.CreateVersion7().ToString("N")[..3].ToUpperInvariant(), "Allocation Currency", true);
        var billingAccount = BillingAccount.Create(_tenantId, BillingAccountOwnerType.Stakeholder, stakeholder.Id, "Allocation Owner");
        var plan = SubscriptionPlan.Create($"ALLOC_{Guid.CreateVersion7():N}"[..30], "Allocation Monthly", 5000m, currency.Id, BillingInterval.Monthly, true);
        dbContext.Tenants.Add(tenant);
        dbContext.StakeholderTypes.Add(stakeholderType);
        dbContext.Stakeholders.Add(stakeholder);
        dbContext.Currencies.Add(currency);
        dbContext.BillingAccounts.Add(billingAccount);
        dbContext.SubscriptionPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        _userId = user.Id;
        _stakeholderId = stakeholder.Id;
        _currencyId = currency.Id;
        _billingAccountId = billingAccount.Id;
        _planId = plan.Id;
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        _merchantReference = $"pay_{Guid.CreateVersion7():N}";
        var payment = PaymentTransaction.Create(
            _merchantReference, PaymentIntent.Subscription, Guid.CreateVersion7(), plan.Price, currency.Id, stakeholder.CountryId, user.Id, stakeholder.Id, _tenantId);
        payment.MarkInitiated("provider-ref", null, null, "initiated");
        payment.MarkSucceeded("provider-ref", "confirmed", now);
        var paid = Domain.Subscriptions.Entities.Subscription.CreatePending(
            _tenantId, billingAccount.Id, plan.Id, true, now, Guid.CreateVersion7(), stakeholder.Id.ToString());
        var purchase = SubscriptionPurchase.Create(_tenantId, billingAccount.Id, paid.Id, plan, payment.Id, stakeholder.Id);
        dbContext.PaymentTransactions.Add(payment);
        dbContext.Subscriptions.Add(paid);
        dbContext.SubscriptionPurchases.Add(purchase);
        await dbContext.SaveChangesAsync();
        _paidSubscriptionId = paid.Id;
        _purchaseId = purchase.Id;
        _paymentId = payment.Id;
        _plan = plan;
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateDbContextScope();
        var db = scope.DbContext;
        var subscriptionIds = await db.Subscriptions.Where(item => item.TenantId == _tenantId).Select(item => item.Id).ToListAsync();
        var tenantText = _tenantId.ToString();
        await db.OutboxMessages.Where(item => item.Payload.Contains(tenantText)).ExecuteDeleteAsync();
        await db.MessageInboxes.Where(item => item.MessageId == _messageId).ExecuteDeleteAsync();
        await db.SubscriptionActivations.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await db.SubscriptionAllocations.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await db.SubscriptionStatusHistory.Where(item => subscriptionIds.Contains(item.SubscriptionId)).ExecuteDeleteAsync();
        await db.SubscriptionPeriods.Where(item => subscriptionIds.Contains(item.SubscriptionId)).ExecuteDeleteAsync();
        await db.SubscriptionPurchases.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await db.Subscriptions.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await db.PaymentTransactions.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await db.BillingAccounts.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await db.SubscriptionPlans.Where(item => item.Id == _planId).ExecuteDeleteAsync();
        await db.Currencies.Where(item => item.Id == _currencyId).ExecuteDeleteAsync();
        await db.Stakeholders.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await db.StakeholderTypes.Where(item => item.TenantId == _tenantId).ExecuteDeleteAsync();
        await db.Users.Where(item => item.Id == _userId).ExecuteDeleteAsync();
        await db.Tenants.Where(item => item.Id == _tenantId).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task PreservePaymentAndBlockFulfillmentWhenAnAllocationAlreadyExists()
    {
        await CommitManualAllocationAsync();

        await HandlePaymentAsync();

        await ThenPaymentIsPreservedAndFulfillmentBlockedAsync();
    }

    [Fact]
    public async Task BlockFulfillmentWhenAConcurrentAllocationCommitsFirst()
    {
        using var allocationScope = CreateDbContextScope();
        await using var allocationTransaction = await allocationScope.DbContext.Database.BeginTransactionAsync();
        allocationScope.DbContext.Subscriptions.Add(CreateManualAllocation());
        await allocationScope.DbContext.SaveChangesAsync();

        // The paid fulfillment cannot see the uncommitted grant, passes the application check, and its write
        // waits on the exclusion constraint until the competing transaction resolves.
        var fulfillment = HandlePaymentAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
        fulfillment.IsCompleted.ShouldBeFalse();
        await allocationTransaction.CommitAsync();
        await fulfillment;

        await ThenPaymentIsPreservedAndFulfillmentBlockedAsync();
    }

    private Domain.Subscriptions.Entities.Subscription CreateManualAllocation()
    {
        using var scope = CreateScope();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        return Domain.Subscriptions.Entities.Subscription.CreateAllocated(
            _tenantId, _billingAccountId, _plan, now, now.AddDays(30), now, Guid.CreateVersion7(), Guid.CreateVersion7().ToString(), "Manual grant");
    }

    private async Task CommitManualAllocationAsync()
    {
        using var scope = CreateDbContextScope();
        scope.DbContext.Subscriptions.Add(CreateManualAllocation());
        await scope.DbContext.SaveChangesAsync();
    }

    private async Task HandlePaymentAsync()
    {
        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ActivateSubscriptionHandler>().HandleAsync(
            new ActivateSubscriptionCommand(_paymentId, _merchantReference, 5000m, _currencyId)
            {
                MessageId = _messageId,
                TenantId = _tenantId,
                StakeholderId = _stakeholderId
            },
            CancellationToken.None);
    }

    private async Task ThenPaymentIsPreservedAndFulfillmentBlockedAsync()
    {
        using var dbScope = CreateDbContextScope();
        var purchase = await dbScope.DbContext.SubscriptionPurchases.SingleAsync(item => item.Id == _purchaseId);
        purchase.FulfilledAtUtc.ShouldBeNull();
        purchase.FulfillmentBlockedAtUtc.ShouldNotBeNull();
        purchase.FulfillmentBlockedReason.ShouldBe(SubscriptionFulfillmentBlockReasons.EntitlementConflict);
        var paid = await dbScope.DbContext.Subscriptions.SingleAsync(item => item.Id == _paidSubscriptionId);
        paid.Status.ShouldBe(SubscriptionStatus.Pending);
        (await dbScope.DbContext.SubscriptionPeriods.AnyAsync(item => item.SubscriptionId == _paidSubscriptionId)).ShouldBeFalse();
        (await dbScope.DbContext.PaymentTransactions.SingleAsync(item => item.Id == _paymentId)).PaymentStatus.ShouldBe(PaymentStatus.Succeeded);
        (await dbScope.DbContext.SubscriptionActivations.AnyAsync(item => item.PaymentTransactionId == _paymentId)).ShouldBeFalse();
        (await dbScope.DbContext.Subscriptions.CountAsync(item => item.TenantId == _tenantId && item.Status == SubscriptionStatus.Active)).ShouldBe(1);
        (await dbScope.DbContext.MessageInboxes.AnyAsync(item => item.MessageId == _messageId)).ShouldBeTrue();
    }
}
