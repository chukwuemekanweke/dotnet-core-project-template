using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;

[Collection(nameof(ContainersCollection))]
public sealed class When_GrantingEntitlements_Concurrently_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task AllowExactlyOneOverlappingEntitlementAndOneAllocationPerIdempotencyKey()
    {
        await GrantRoleAsync(ApplicationRoles.Administrator);
        await AuthenticateAsync();
        var start = UtcNow().AddDays(30);

        var competing = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(_ => AllocateAsync(TenantAllocation(start, start.AddDays(10)))));
        competing.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        competing.Count(response => response.StatusCode == HttpStatusCode.Conflict).ShouldBe(5);
        foreach (var response in competing)
        {
            response.Dispose();
        }

        var key = Guid.CreateVersion7();
        var duplicateStart = start.AddDays(40);
        var duplicates = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => AllocateAsync(TenantAllocation(duplicateStart, duplicateStart.AddDays(5), key))));
        duplicates.ShouldAllBe(response => response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
        var subscriptionIds = new HashSet<Guid>();
        foreach (var response in duplicates)
        {
            subscriptionIds.Add((await response.Content.ReadFromJsonAsync<AllocateSubscriptionResponse>())!.SubscriptionId);
            response.Dispose();
        }

        subscriptionIds.Count.ShouldBe(1);
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.SubscriptionAllocations.CountAsync(item => item.OperationId == key)).ShouldBe(1);
        (await dbContext.Subscriptions.CountAsync(item => item.TenantId == TargetTenantId)).ShouldBe(2);
    }

    [Fact]
    public async Task ResolveOneBillingAccountPerOwnerUnderConcurrency()
    {
        var accounts = await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            using var scope = CreateScope();
            var resolver = scope.ServiceProvider.GetRequiredService<Domain.Payments.IBillingAccountResolver>();
            return await resolver.ResolveOrCreateAsync(
                TargetTenantId,
                Domain.Payments.BillingAccountOwnerType.Stakeholder,
                TargetStakeholderId,
                CancellationToken.None);
        }));

        accounts.Select(account => account.Id).Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public async Task RejectOverlapsAtTheDatabaseIncludingPaidFulfillmentThatBypassesApplicationChecks()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var resolver = scope.ServiceProvider.GetRequiredService<Domain.Payments.IBillingAccountResolver>();
        var account = await resolver.ResolveOrCreateAsync(TargetTenantId, Domain.Payments.BillingAccountOwnerType.Tenant, null, CancellationToken.None);
        var plan = await dbContext.SubscriptionPlans.SingleAsync(item => item.Id == PlanId);
        var now = UtcNow();
        var actor = AdminStakeholderId.ToString();

        var first = Domain.Subscriptions.Entities.Subscription.CreateAllocated(
            TargetTenantId, account.Id, plan, now.AddDays(1), now.AddDays(11), now, Guid.CreateVersion7(), actor, "first");
        dbContext.Subscriptions.Add(first);
        await dbContext.SaveChangesAsync();

        var adjacent = Domain.Subscriptions.Entities.Subscription.CreateAllocated(
            TargetTenantId, account.Id, plan, now.AddDays(11), now.AddDays(12), now, Guid.CreateVersion7(), actor, "adjacent");
        dbContext.Subscriptions.Add(adjacent);
        await dbContext.SaveChangesAsync();

        var overlapping = Domain.Subscriptions.Entities.Subscription.CreateAllocated(
            TargetTenantId, account.Id, plan, now.AddDays(5), now.AddDays(6), now, Guid.CreateVersion7(), actor, "overlap");
        dbContext.Subscriptions.Add(overlapping);
        await Should.ThrowAsync<Domain.Subscriptions.Exceptions.SubscriptionEntitlementConflictException>(() => dbContext.SaveChangesAsync());
        dbContext.Entry(overlapping).State = EntityState.Detached;
        foreach (var history in overlapping.StatusHistory)
        {
            dbContext.Entry(history).State = EntityState.Detached;
        }

        var payment = Domain.Payments.Entities.PaymentTransaction.Create(
            $"pay_{Guid.CreateVersion7():N}",
            Contracts.Payments.PaymentIntent.Subscription,
            Guid.CreateVersion7(),
            plan.Price,
            plan.CurrencyId,
            Guid.CreateVersion7(),
            AdminUserId,
            TargetStakeholderId,
            TargetTenantId);
        payment.MarkInitiated("provider", null, null, "initiated");
        payment.MarkSucceeded("provider", "confirmed", now);
        var paid = Domain.Subscriptions.Entities.Subscription.CreatePending(
            TargetTenantId, account.Id, plan.Id, true, now, Guid.CreateVersion7(), TargetStakeholderId.ToString());
        dbContext.PaymentTransactions.Add(payment);
        dbContext.Subscriptions.Add(paid);
        await dbContext.SaveChangesAsync();
        var purchase = Domain.Subscriptions.Entities.SubscriptionPurchase.Create(
            TargetTenantId, account.Id, paid.Id, plan, payment.Id, TargetStakeholderId);
        var period = paid.ActivateFromPayment(purchase, payment.Id, now, purchase.Id);
        dbContext.SubscriptionPeriods.Add(period);

        await Should.ThrowAsync<Domain.Subscriptions.Exceptions.SubscriptionEntitlementConflictException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task ApplyTheMigrationConstraints()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exclusion = await dbContext.Database
            .SqlQueryRaw<string>("SELECT pg_get_constraintdef(oid) AS \"Value\" FROM pg_constraint WHERE conname = 'EX_Subscriptions_BillingAccountEntitlement'")
            .SingleAsync();
        var activationSource = await dbContext.Database
            .SqlQueryRaw<string>("SELECT pg_get_constraintdef(oid) AS \"Value\" FROM pg_constraint WHERE conname = 'CK_SubscriptionPeriods_ActivationSource'")
            .SingleAsync();
        var status = await dbContext.Database
            .SqlQueryRaw<string>("SELECT pg_get_constraintdef(oid) AS \"Value\" FROM pg_constraint WHERE conname = 'CK_Subscriptions_Status'")
            .SingleAsync();

        exclusion.ShouldContain("EXCLUDE USING gist");
        exclusion.ShouldContain("tstzrange");
        activationSource.ShouldContain("3");
        status.ShouldContain("5");
        (await dbContext.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
    }
}
