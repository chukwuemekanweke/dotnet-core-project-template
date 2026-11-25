using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsTimeline;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AdminSubscriptionAnalytics;

public sealed class When_GettingAdminSubscriptionAnalyticsTimeline_WithPartialFinalBucket_Should
{
    [Fact]
    public async Task CapTheBucketAndDeriveArrPerCurrency()
    {
        var repository = Substitute.For<ISubscriptionAnalyticsReadModelRepository>();
        var currencyId = Guid.CreateVersion7();
        repository.GetTimelineAsync(Arg.Any<SubscriptionAnalyticsFilter>(), Interval.Week, Arg.Any<CancellationToken>())
            .Returns(
            [
                new SubscriptionAnalyticsTimelineRow(new DateOnly(2026, 11, 2), 1, 1, 0, 0, 0, [new(currencyId, "NGN", 0m)]),
                new SubscriptionAnalyticsTimelineRow(new DateOnly(2026, 11, 9), 2, 1, 0, 1, 0, [new(currencyId, "NGN", 250m)])
            ]);
        var handler = new GetAdminSubscriptionAnalyticsTimelineHandler(
            repository,
            SubscriptionAnalyticsHandlerTestContext.CreateCache(),
            SubscriptionAnalyticsHandlerTestContext.CreateTimeProvider(),
            Options.Create(new SubscriptionAnalyticsOptions()));

        var result = await handler.HandleAsync(
            new SubscriptionAnalyticsQuery(new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 11), null, null, null, null),
            Interval.Week,
            CancellationToken.None);

        result.Interval.ShouldBe("Week");
        result.Buckets[0].PeriodEnd.ShouldBe(new DateOnly(2026, 11, 8));
        result.Buckets[1].PeriodEnd.ShouldBe(new DateOnly(2026, 11, 11));
        result.Buckets[0].Revenues.ShouldHaveSingleItem().Arr.ShouldBe(0m);
        result.Buckets[1].Revenues.ShouldHaveSingleItem().Arr.ShouldBe(3_000m);
    }
}
