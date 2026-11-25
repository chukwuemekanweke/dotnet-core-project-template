using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsSummary;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AdminSubscriptionAnalytics;

public sealed class When_GettingAdminSubscriptionAnalytics_WithDistinctFilters_Should
{
    [Fact]
    public async Task IsolateCachedResults()
    {
        var repository = Substitute.For<ISubscriptionAnalyticsReadModelRepository>();
        repository.GetSummaryAsync(Arg.Any<SubscriptionAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(new SubscriptionAnalyticsSummaryReadModel(0, 0, 0, 0, 0, 0, 0, []));
        var handler = new GetAdminSubscriptionAnalyticsSummaryHandler(
            repository,
            SubscriptionAnalyticsHandlerTestContext.CreateCache(),
            SubscriptionAnalyticsHandlerTestContext.CreateTimeProvider(),
            Options.Create(new SubscriptionAnalyticsOptions()));
        var baseline = new SubscriptionAnalyticsQuery(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 10),
            null,
            null,
            null,
            null);
        SubscriptionAnalyticsQuery[] variants =
        [
            baseline,
            baseline with { To = new DateOnly(2026, 11, 11) },
            baseline with { From = new DateOnly(2026, 11, 2) },
            baseline with { PlanId = Guid.CreateVersion7() },
            baseline with { OwnerType = BillingAccountOwnerType.Tenant },
            baseline with { OwnerType = BillingAccountOwnerType.Stakeholder },
            baseline with { TenantId = Guid.CreateVersion7() },
            baseline with { CurrencyId = Guid.CreateVersion7() }
        ];

        foreach (var variant in variants)
            await handler.HandleAsync(variant, CancellationToken.None);
        await handler.HandleAsync(baseline, CancellationToken.None);

        await repository.Received(variants.Length).GetSummaryAsync(
            Arg.Any<SubscriptionAnalyticsFilter>(),
            Arg.Any<CancellationToken>());
    }
}
