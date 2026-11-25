using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Subscriptions;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions.Analytics;

[Collection(nameof(ContainersCollection))]
public sealed class When_FilteringSubscriptionAnalytics_Should(ContainersFixture fixture)
    : SubscriptionAnalyticsIntegrationTestBase(fixture)
{
    [Fact]
    public async Task AttributeChurnPerBillingAccountAndKeepFiltersConsistent()
    {
        // Given: one customer switches from ALPHA to BETA on the same BillingAccount (no churn), another
        // cancels ALPHA without a replacement (churn), and the tenant holds a complimentary allocation.
        var alpha = await CreatePlanAsync("ALPHA", 100m, BillingInterval.Monthly, CurrencyId);
        var beta = await CreatePlanAsync("BETA", 200m, BillingInterval.Monthly, CurrencyId);
        var unusedCurrencyId = await CreateCurrencyAsync();
        var switchingOwnerId = await AddTargetStakeholderAsync();
        await SeedPaidSubscriptionAsync(alpha, DaysAgo(20), autoRenew: true,
            new PaidLifecycle(CancelImmediatelyAtUtc: DaysAgo(6), StakeholderId: switchingOwnerId));
        await SeedPaidSubscriptionAsync(beta, DaysAgo(6), autoRenew: true,
            new PaidLifecycle(StakeholderId: switchingOwnerId));
        await SeedPaidSubscriptionAsync(alpha, DaysAgo(20), autoRenew: false,
            new PaidLifecycle(CancelImmediatelyAtUtc: DaysAgo(4)));
        await AllocateSuccessfullyAsync(TenantAllocation(endAtUtc: UtcNow().AddDays(10)));
        var range = $"from={Today.AddDays(-30):yyyy-MM-dd}&to={Today:yyyy-MM-dd}&tenantId={TargetTenantId}";

        // When / Then
        await AssertSliceAsync(range, active: 2, created: 4, churned: 1, mrr: 200m);
        await AssertSliceAsync($"{range}&planId={alpha.Id}", active: 0, created: 2, churned: 1, mrr: null);
        await AssertSliceAsync($"{range}&planId={beta.Id}", active: 1, created: 1, churned: 0, mrr: 200m);
        await AssertSliceAsync($"{range}&ownerType=Tenant", active: 1, created: 1, churned: 0, mrr: null);
        await AssertSliceAsync($"{range}&ownerType=Stakeholder", active: 1, created: 3, churned: 1, mrr: 200m);
        await AssertSliceAsync($"{range}&currencyId={CurrencyId}", active: 2, created: 4, churned: 1, mrr: 200m);

        var alphaStatuses = await GetAsync<SubscriptionAnalyticsStatusesResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.StatusesV1}?{range}&planId={alpha.Id}");
        alphaStatuses.Statuses.ShouldHaveSingleItem().Status.ShouldBe("Cancelled");
        alphaStatuses.Statuses.Single().SubscriptionCount.ShouldBe(2);
        alphaStatuses.Statuses.Single().Percentage.ShouldBe(100m);

        // Then: an empty slice keeps every bucket, zero-valued, and serializes stable empty collections.
        var emptyQuery = $"{range}&currencyId={unusedCurrencyId}";
        var emptyTimeline = await GetAsync<SubscriptionAnalyticsTimelineResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.TimelineV1}?{emptyQuery}&interval=Week");
        emptyTimeline.Buckets.ShouldNotBeEmpty();
        emptyTimeline.Buckets.ShouldAllBe(item =>
            item.ActiveSubscriptions == 0 && item.NewSubscriptions == 0 && item.ChurnedSubscriptions == 0 &&
            item.SuccessfulRenewals == 0 && item.FailedRenewals == 0 && item.Revenues.Count == 0);
        (await GetAsync<SubscriptionAnalyticsPlansResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.PlansV1}?{emptyQuery}")).Plans.ShouldBeEmpty();
        (await GetAsync<SubscriptionAnalyticsStatusesResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.StatusesV1}?{emptyQuery}")).Statuses.ShouldBeEmpty();

        using var raw = await Client.GetAsync($"{EndpointUrl.AdminSubscriptionAnalytics.SummaryV1}?{emptyQuery}");
        raw.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await raw.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("from").GetString().ShouldBe($"{Today.AddDays(-30):yyyy-MM-dd}");
        json.RootElement.GetProperty("activeSubscriptions").GetInt64().ShouldBe(0);
        json.RootElement.GetProperty("renewalSuccessRate").GetDecimal().ShouldBe(0m);
        json.RootElement.GetProperty("revenues").GetArrayLength().ShouldBe(0);
        json.RootElement.TryGetProperty("snapshotAtUtc", out _).ShouldBeTrue();
    }

    private async Task AssertSliceAsync(string query, long active, long created, long churned, decimal? mrr)
    {
        var summary = await GetAsync<SubscriptionAnalyticsSummaryResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.SummaryV1}?{query}");
        var timeline = await GetAsync<SubscriptionAnalyticsTimelineResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.TimelineV1}?{query}&interval=Week");
        var plans = await GetAsync<SubscriptionAnalyticsPlansResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.PlansV1}?{query}");

        summary.ActiveSubscriptions.ShouldBe(active, query);
        summary.NewSubscriptions.ShouldBe(created, query);
        summary.ChurnedSubscriptions.ShouldBe(churned, query);
        if (mrr is null)
            summary.Revenues.ShouldBeEmpty(query);
        else
            summary.Revenues.ShouldHaveSingleItem(query).Mrr.ShouldBe(mrr.Value, query);

        timeline.Buckets[^1].ActiveSubscriptions.ShouldBe(active, query);
        timeline.Buckets.Sum(item => item.NewSubscriptions).ShouldBe(created, query);
        timeline.Buckets.Sum(item => item.ChurnedSubscriptions).ShouldBe(churned, query);
        timeline.Buckets[^1].Revenues.Where(item => item.Mrr != 0m).ShouldBe(summary.Revenues, query);
        plans.Plans.Sum(item => item.SubscriptionCount).ShouldBe(active, query);
    }
}
