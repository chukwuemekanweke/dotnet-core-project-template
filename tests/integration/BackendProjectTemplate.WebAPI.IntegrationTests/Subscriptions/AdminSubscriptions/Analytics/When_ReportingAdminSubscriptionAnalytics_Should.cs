using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions.Analytics;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReportingAdminSubscriptionAnalytics_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task AuthorizeFilterAndExcludeComplimentaryAllocationRevenue()
    {
        using (var anonymous = await Client.GetAsync(EndpointUrl.AdminSubscriptionAnalytics.SummaryV1))
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        await AuthenticateAsync();
        using (var forbidden = await Client.GetAsync(EndpointUrl.AdminSubscriptionAnalytics.SummaryV1))
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await GrantRoleAsync(ApplicationRoles.Administrator);
        var now = UtcNow();
        await AllocateSuccessfullyAsync(TenantAllocation(endAtUtc: now.AddDays(10)));
        var annualPlan = await CreatePlanAsync("ANNUAL_ANALYTICS", 1_200m, BillingInterval.Yearly, true);
        await SeedPaidNonRenewingSubscriptionAsync(annualPlan, now);
        var date = DateOnly.FromDateTime(now.UtcDateTime);
        var query = $"from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}&tenantId={TargetTenantId}";

        using var summaryResponse = await Client.GetAsync($"{EndpointUrl.AdminSubscriptionAnalytics.SummaryV1}?{query}");
        summaryResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await summaryResponse.Content.ReadAsStringAsync());
        var summary = (await summaryResponse.Content.ReadFromJsonAsync<SubscriptionAnalyticsSummaryResponse>())!;
        summary.ActiveSubscriptions.ShouldBe(2);
        summary.NewSubscriptions.ShouldBe(2);
        summary.Revenues.ShouldHaveSingleItem().Mrr.ShouldBe(100m);
        summary.Revenues.ShouldHaveSingleItem().Arr.ShouldBe(1_200m);

        using var plansResponse = await Client.GetAsync($"{EndpointUrl.AdminSubscriptionAnalytics.PlansV1}?{query}");
        var plans = (await plansResponse.Content.ReadFromJsonAsync<SubscriptionAnalyticsPlansResponse>())!;
        plans.Plans.Count.ShouldBe(2);
        plans.Plans.Single(item => item.PlanId == PlanId).SubscriptionCount.ShouldBe(1);
        plans.Plans.Single(item => item.PlanId == annualPlan.Id).SubscriptionCount.ShouldBe(1);

        using var statusesResponse = await Client.GetAsync($"{EndpointUrl.AdminSubscriptionAnalytics.StatusesV1}?{query}");
        var statuses = (await statusesResponse.Content.ReadFromJsonAsync<SubscriptionAnalyticsStatusesResponse>())!;
        statuses.Statuses.ShouldHaveSingleItem().SubscriptionCount.ShouldBe(2);
        statuses.Statuses.ShouldHaveSingleItem().Status.ShouldBe("Active");

        using var timelineResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminSubscriptionAnalytics.TimelineV1}?{query}&interval=Day");
        var timeline = (await timelineResponse.Content.ReadFromJsonAsync<SubscriptionAnalyticsTimelineResponse>())!;
        timeline.Buckets.ShouldHaveSingleItem().ActiveSubscriptions.ShouldBe(2);
        timeline.Buckets.ShouldHaveSingleItem().Revenues.ShouldHaveSingleItem().Mrr.ShouldBe(100m);

        using var invalidResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminSubscriptionAnalytics.SummaryV1}?from=2026-12-02&to=2026-12-01");
        invalidResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task SeedPaidNonRenewingSubscriptionAsync(SubscriptionPlan plan, DateTimeOffset activatedAtUtc)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stakeholder = await dbContext.Stakeholders.SingleAsync(item => item.Id == TargetStakeholderId);
        var provider = PaymentProvider.Create(
            "Subscription Analytics Provider",
            $"subscription-analytics-{Guid.CreateVersion7():N}",
            true);
        TrackPaymentProviderForCleanup(provider.Id);
        var account = BillingAccount.Create(
            TargetTenantId,
            BillingAccountOwnerType.Stakeholder,
            TargetStakeholderId,
            "Analytics stakeholder account");
        var payment = PaymentTransaction.Create(
            $"analytics-{Guid.CreateVersion7():N}",
            PaymentIntent.Subscription,
            provider.Id,
            plan.Price,
            plan.CurrencyId,
            stakeholder.CountryId,
            stakeholder.AppUserId,
            stakeholder.Id,
            TargetTenantId);
        var purchaseOperationId = Guid.CreateVersion7();
        var activationOperationId = Guid.CreateVersion7();
        var subscription = Subscription.CreatePending(
            TargetTenantId,
            account.Id,
            plan.Id,
            autoRenew: false,
            activatedAtUtc.AddMilliseconds(-1),
            purchaseOperationId,
            stakeholder.Id.ToString());
        var purchase = SubscriptionPurchase.Create(
            TargetTenantId,
            account.Id,
            subscription.Id,
            plan,
            payment.Id,
            stakeholder.Id);
        subscription.ActivateFromPayment(purchase, payment.Id, activatedAtUtc, activationOperationId);
        purchase.MarkFulfilled(activatedAtUtc);

        dbContext.BillingAccounts.Add(account);
        dbContext.PaymentProviders.Add(provider);
        dbContext.PaymentTransactions.Add(payment);
        dbContext.Subscriptions.Add(subscription);
        dbContext.SubscriptionPurchases.Add(purchase);
        await dbContext.SaveChangesAsync();
    }

}
