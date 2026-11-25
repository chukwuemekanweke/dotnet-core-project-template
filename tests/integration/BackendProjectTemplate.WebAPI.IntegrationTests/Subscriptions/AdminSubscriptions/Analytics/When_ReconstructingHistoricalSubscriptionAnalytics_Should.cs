using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Subscriptions;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions.Analytics;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReconstructingHistoricalSubscriptionAnalytics_Should(ContainersFixture fixture)
    : SubscriptionAnalyticsIntegrationTestBase(fixture)
{
    [Fact]
    public async Task ReportHistoricalStateAndReconcileSummaryWithEveryTimelineInterval()
    {
        // Given: monthly plans priced 100 in one currency, a 1 200 annual plan in a second currency, and
        // these lifecycles in the target tenant (D = today, every event at noon UTC):
        //   recovered   monthly, auto-renew, active D-40; first renewal attempt declined, retry paid
        //   annual      yearly,  non-renewing, active D-20 (second currency)
        //   expired     monthly, auto-renew, active D-50; renewal exhausted, ended D-15 (no replacement)
        //   cancelling  monthly, auto-renew, active D-10; cancellation at period end requested D-5
        //   replaced    monthly, auto-renew, active D-40; cancelled immediately D-13 and replaced at that instant
        //   replacement monthly, non-renewing, same BillingAccount, active D-13
        //   cancelled   monthly, non-renewing, active D-25; cancelled immediately D-8 (no replacement)
        //   unpaid      purchase expired D-3 without ever being paid
        //   allocation  complimentary allocation of the 750 000 base plan, active now
        var secondCurrencyId = await CreateCurrencyAsync();
        var monthly = await CreatePlanAsync("MONTHLY", 100m, BillingInterval.Monthly, CurrencyId);
        var annual = await CreatePlanAsync("ANNUAL", 1_200m, BillingInterval.Yearly, secondCurrencyId);
        await SeedPaidSubscriptionAsync(monthly, DaysAgo(40), autoRenew: true,
            new PaidLifecycle(Renewal: RenewalOutcome.RecoveredAfterFailure));
        await SeedPaidSubscriptionAsync(annual, DaysAgo(20), autoRenew: false);
        await SeedPaidSubscriptionAsync(monthly, DaysAgo(50), autoRenew: true,
            new PaidLifecycle(Renewal: RenewalOutcome.Exhausted, EndAtUtc: DaysAgo(15)));
        await SeedPaidSubscriptionAsync(monthly, DaysAgo(10), autoRenew: true,
            new PaidLifecycle(ScheduleCancellationAtUtc: DaysAgo(5)));
        var replacedOwnerId = await AddTargetStakeholderAsync();
        await SeedPaidSubscriptionAsync(monthly, DaysAgo(40), autoRenew: true,
            new PaidLifecycle(CancelImmediatelyAtUtc: DaysAgo(13), StakeholderId: replacedOwnerId));
        await SeedPaidSubscriptionAsync(monthly, DaysAgo(13), autoRenew: false,
            new PaidLifecycle(StakeholderId: replacedOwnerId));
        await SeedPaidSubscriptionAsync(monthly, DaysAgo(25), autoRenew: false,
            new PaidLifecycle(CancelImmediatelyAtUtc: DaysAgo(8)));
        await SeedUnpaidExpiredPurchaseAsync(monthly, DaysAgo(3));
        await AllocateSuccessfullyAsync(TenantAllocation(endAtUtc: UtcNow().AddDays(25)));
        // Repricing the plan later must not rewrite revenue from the immutable period snapshots.
        await RepricePlanAsync(monthly.Id, 999m);

        var range = $"from={Today.AddDays(-30):yyyy-MM-dd}&to={Today:yyyy-MM-dd}&tenantId={TargetTenantId}";

        // When
        var summary = await GetAsync<SubscriptionAnalyticsSummaryResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.SummaryV1}?{range}");
        var historical = await GetAsync<SubscriptionAnalyticsSummaryResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.SummaryV1}?from={Today.AddDays(-30):yyyy-MM-dd}&to={Today.AddDays(-17):yyyy-MM-dd}&tenantId={TargetTenantId}");
        var renewals = await GetAsync<SubscriptionAnalyticsRenewalsResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.RenewalsV1}?{range}");
        var plans = await GetAsync<SubscriptionAnalyticsPlansResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.PlansV1}?{range}");
        var statuses = await GetAsync<SubscriptionAnalyticsStatusesResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.StatusesV1}?{range}");

        // Then: snapshot at the end of today
        summary.ActiveSubscriptions.ShouldBe(5); // recovered, annual, cancelling, replacement, allocation
        summary.NewSubscriptions.ShouldBe(5); // annual, cancelling, replacement, cancelled, allocation
        summary.ChurnedSubscriptions.ShouldBe(2); // expired, cancelled; the replaced subscription is not churn
        summary.PastDueSubscriptions.ShouldBe(0);
        summary.CancelAtPeriodEndSubscriptions.ShouldBe(1);
        summary.SuccessfulRenewals.ShouldBe(1);
        summary.FailedRenewals.ShouldBe(1);
        summary.RenewalSuccessRate.ShouldBe(50m);
        AssertRevenue(summary.Revenues, CurrencyId, 300m); // complimentary allocation excluded
        AssertRevenue(summary.Revenues, secondCurrencyId, 100m); // 1 200 yearly / 12

        // Then: the same population as it stood at the end of D-17, despite every later change
        historical.ActiveSubscriptions.ShouldBe(5); // recovered, annual, expired, replaced, cancelled
        historical.PastDueSubscriptions.ShouldBe(1); // expired was in renewal grace
        historical.CancelAtPeriodEndSubscriptions.ShouldBe(0);
        AssertRevenue(historical.Revenues, CurrencyId, 400m);
        AssertRevenue(historical.Revenues, secondCurrencyId, 100m);

        renewals.SuccessfulRenewals.ShouldBe(1);
        renewals.FailedRenewals.ShouldBe(1);
        renewals.FailedPaymentAttempts.ShouldBe(3); // the recovered decline is an attempt, not a failed renewal
        renewals.PendingRenewals.ShouldBe(0);

        plans.Plans.Sum(item => item.SubscriptionCount).ShouldBe(summary.ActiveSubscriptions);
        plans.Plans[0].PlanId.ShouldBe(monthly.Id);
        plans.Plans[0].SubscriptionCount.ShouldBe(3);
        plans.Plans.Single(item => item.PlanId == annual.Id).SubscriptionCount.ShouldBe(1);
        plans.Plans.Single(item => item.PlanId == PlanId).SubscriptionCount.ShouldBe(1);

        statuses.Statuses.Select(item => (item.Status, item.SubscriptionCount))
            .ShouldBe([("Active", 5L), ("Cancelled", 2L), ("Expired", 2L)]);

        foreach (var interval in new[] { "Day", "Week", "Month" })
        {
            var timeline = await GetAsync<SubscriptionAnalyticsTimelineResponse>(
                $"{EndpointUrl.AdminSubscriptionAnalytics.TimelineV1}?{range}&interval={interval}");
            timeline.Interval.ShouldBe(interval);
            timeline.Buckets[^1].PeriodEnd.ShouldBe(Today);
            timeline.Buckets[^1].ActiveSubscriptions.ShouldBe(summary.ActiveSubscriptions);
            timeline.Buckets.Sum(item => item.NewSubscriptions).ShouldBe(summary.NewSubscriptions);
            timeline.Buckets.Sum(item => item.ChurnedSubscriptions).ShouldBe(summary.ChurnedSubscriptions);
            timeline.Buckets.Sum(item => item.SuccessfulRenewals).ShouldBe(summary.SuccessfulRenewals);
            timeline.Buckets.Sum(item => item.FailedRenewals).ShouldBe(summary.FailedRenewals);
            timeline.Buckets[^1].Revenues.ShouldBe(summary.Revenues, ignoreOrder: false);
        }

        var daily = await GetAsync<SubscriptionAnalyticsTimelineResponse>(
            $"{EndpointUrl.AdminSubscriptionAnalytics.TimelineV1}?{range}&interval=Day");
        daily.Buckets.Count.ShouldBe(31);
        daily.Buckets.Select(item => item.PeriodStart).ShouldBe(
            Enumerable.Range(0, 31).Select(offset => Today.AddDays(offset - 30)));
        var atD17 = daily.Buckets.Single(item => item.PeriodStart == Today.AddDays(-17));
        atD17.ActiveSubscriptions.ShouldBe(historical.ActiveSubscriptions);
        atD17.Revenues.ShouldBe(historical.Revenues, ignoreOrder: false);
        // Before the annual subscription started its currency is still reported, as zero.
        var first = daily.Buckets[0];
        first.NewSubscriptions.ShouldBe(0);
        first.ChurnedSubscriptions.ShouldBe(0);
        AssertRevenue(first.Revenues, secondCurrencyId, 0m);
    }

    private static void AssertRevenue(
        IReadOnlyList<SubscriptionAnalyticsRevenueResponse> revenues,
        Guid currencyId,
        decimal expectedMrr)
    {
        var revenue = revenues.Single(item => item.CurrencyId == currencyId);
        revenue.Mrr.ShouldBe(expectedMrr);
        revenue.Arr.ShouldBe(expectedMrr * 12m);
    }
}
