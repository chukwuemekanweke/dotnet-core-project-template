using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;

[Collection(nameof(ContainersCollection))]
public sealed class When_AllocatingSubscription_WithAdministrator_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task GrantComplimentaryEntitlementsWithoutPaymentRecordsAndEnforceOneEntitlement()
    {
        await GrantRoleAsync(ApplicationRoles.Administrator);
        await AuthenticateAsync();
        var now = UtcNow();

        var key = Guid.CreateVersion7();
        var immediate = await AllocateSuccessfullyAsync(TenantAllocation(endAtUtc: now.AddDays(10), idempotencyKey: key));
        immediate.Status.ShouldBe(nameof(SubscriptionStatus.Active));
        immediate.AutoRenew.ShouldBeFalse();

        using (var replay = await AllocateAsync(TenantAllocation(endAtUtc: now.AddDays(10), idempotencyKey: key)))
        {
            replay.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await replay.Content.ReadFromJsonAsync<AllocateSubscriptionResponse>())!.SubscriptionId.ShouldBe(immediate.SubscriptionId);
        }

        using (var reusedKey = await AllocateAsync(TenantAllocation(endAtUtc: now.AddDays(11), idempotencyKey: key)))
        {
            reusedKey.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        using (var overlapping = await AllocateAsync(TenantAllocation(now.AddDays(5), now.AddDays(20))))
        {
            overlapping.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        var adjacent = await AllocateSuccessfullyAsync(TenantAllocation(immediate.EntitlementEndUtc, immediate.EntitlementEndUtc.AddDays(5)));
        adjacent.Status.ShouldBe(nameof(SubscriptionStatus.Scheduled));
        adjacent.EntitlementStartUtc.ShouldBe(immediate.EntitlementEndUtc);

        var stakeholderGrant = await AllocateSuccessfullyAsync(StakeholderAllocation(TargetStakeholderId));
        stakeholderGrant.BillingAccountId.ShouldNotBe(immediate.BillingAccountId);

        using (var crossTenant = await AllocateAsync(StakeholderAllocation(ForeignStakeholderId)))
        {
            crossTenant.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }

        using (var missingPlan = await AllocateAsync(TenantAllocation(now.AddDays(100)) with { PlanId = Guid.CreateVersion7() }))
        {
            missingPlan.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        var inactivePlan = await CreatePlanAsync("INACTIVE", 1000m, BillingInterval.Monthly, false);
        using (var inactive = await AllocateAsync(TenantAllocation(now.AddDays(100)) with { PlanId = inactivePlan.Id }))
        {
            inactive.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }

        using (var autoRenew = await AllocateAsync(TenantAllocation(now.AddDays(100)) with { AutoRenew = true }))
        {
            autoRenew.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using (var invalidOwner = await AllocateAsync(TenantAllocation(now.AddDays(100)) with { BillingAccountOwnerType = "Wallet" }))
        {
            invalidOwner.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.SubscriptionAllocations.CountAsync(item => item.TenantId == TargetTenantId)).ShouldBe(3);
        var allocation = await dbContext.SubscriptionAllocations.SingleAsync(item => item.OperationId == key);
        allocation.AllocatedByActorId.ShouldBe(AdminStakeholderId.ToString());
        allocation.Reason.ShouldBe("Integration allocation");
        (await dbContext.PaymentTransactions.AnyAsync(item => item.TenantId == TargetTenantId)).ShouldBeFalse();
        (await dbContext.SubscriptionPurchases.AnyAsync(item => item.TenantId == TargetTenantId)).ShouldBeFalse();
        var period = await dbContext.SubscriptionPeriods.SingleAsync(item => item.SubscriptionId == immediate.SubscriptionId);
        period.PaymentTransactionId.ShouldBeNull();
        period.OperationId.ShouldBe(key);
        var subscription = await dbContext.Subscriptions.SingleAsync(item => item.Id == immediate.SubscriptionId);
        subscription.LastSuccessfulPaymentTransactionId.ShouldBeNull();
        var immediateId = immediate.SubscriptionId.ToString();
        var adjacentId = adjacent.SubscriptionId.ToString();
        (await dbContext.OutboxMessages.CountAsync(item => item.Payload.Contains(immediateId) && item.Type.EndsWith("ExpireSubscriptionCommand"))).ShouldBe(1);
        var activation = await dbContext.OutboxMessages.SingleAsync(item =>
            item.Payload.Contains(adjacentId) && item.Type.EndsWith("ActivateAllocatedSubscriptionCommand"));
        activation.AvailableAtUtc.ShouldBe(adjacent.EntitlementStartUtc);
    }
}
