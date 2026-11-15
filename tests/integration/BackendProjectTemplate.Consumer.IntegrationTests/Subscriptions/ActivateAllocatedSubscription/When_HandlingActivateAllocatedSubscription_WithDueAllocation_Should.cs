using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Consumer.Subscriptions;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
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

namespace BackendProjectTemplate.Consumer.IntegrationTests.Subscriptions.ActivateAllocatedSubscription;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingActivateAllocatedSubscription_WithDueAllocation_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _tenantId;
    private Guid _userId;
    private Guid _stakeholderId;
    private Guid _currencyId;
    private Guid _billingAccountId;
    private Guid _planId;
    private Guid _subscriptionId;
    private Guid _allocationId;
    private DateTimeOffset _startUtc;
    private DateTimeOffset _endUtc;

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
        var operationId = Guid.CreateVersion7();
        var subscription = Domain.Subscriptions.Entities.Subscription.CreateAllocated(
            _tenantId, billingAccount.Id, plan, now.AddMinutes(-30), now.AddDays(10), now.AddHours(-1), operationId, Guid.CreateVersion7().ToString(), "Scheduled grant");
        var allocation = SubscriptionAllocation.Create(
            subscription, plan, SubscriptionAllocationSource.Promotion, "Scheduled grant", Guid.CreateVersion7().ToString(), operationId, new string('f', 64));
        plan.SetActive(false);
        dbContext.Subscriptions.Add(subscription);
        dbContext.SubscriptionAllocations.Add(allocation);
        await dbContext.SaveChangesAsync();
        _subscriptionId = subscription.Id;
        _allocationId = allocation.Id;
        _startUtc = allocation.StartAtUtc;
        _endUtc = allocation.EndAtUtc;
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateDbContextScope();
        var db = scope.DbContext;
        var subscriptionIds = await db.Subscriptions.Where(item => item.TenantId == _tenantId).Select(item => item.Id).ToListAsync();
        var tenantText = _tenantId.ToString();
        await db.OutboxMessages.Where(item => item.Payload.Contains(tenantText)).ExecuteDeleteAsync();
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
    public async Task ActivatePersistHistoryScheduleExpiryAndIgnoreRedelivery()
    {
        var command = new ActivateAllocatedSubscriptionCommand(_subscriptionId, _allocationId, _startUtc)
        {
            TenantId = _tenantId,
            FlowId = "allocation-flow"
        };

        await HandleAsync(command);
        await HandleAsync(command);
        await HandleAsync(command with { MessageId = Guid.CreateVersion7() });

        using var scope = CreateDbContextScope();
        var subscription = await scope.DbContext.Subscriptions.SingleAsync(item => item.Id == _subscriptionId);
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.CurrentPeriodStartUtc.ShouldBe(_startUtc);
        subscription.CurrentPeriodEndUtc.ShouldBe(_endUtc);
        subscription.AutoRenew.ShouldBeFalse();
        var period = await scope.DbContext.SubscriptionPeriods.SingleAsync(item => item.SubscriptionId == _subscriptionId);
        period.PaymentTransactionId.ShouldBeNull();
        period.ActivationSource.ShouldBe(SubscriptionActivationSource.AdministrativeAllocation);
        (await scope.DbContext.SubscriptionStatusHistory.CountAsync(item => item.SubscriptionId == _subscriptionId)).ShouldBe(2);
        var subscriptionText = _subscriptionId.ToString();
        var expiry = await scope.DbContext.OutboxMessages.SingleAsync(item =>
            item.Payload.Contains(subscriptionText) && item.Type.EndsWith(nameof(ExpireSubscriptionCommand)));
        expiry.AvailableAtUtc.ShouldBe(_endUtc);
    }

    private async Task HandleAsync(ActivateAllocatedSubscriptionCommand command)
    {
        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ActivateAllocatedSubscriptionHandler>().HandleAsync(command, CancellationToken.None);
    }
}
