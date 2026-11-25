using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Analytics;

public sealed class When_ValidatingAdminSubscriptionAnalytics_WithUnsupportedFilters_Should
{
    [Fact]
    public async Task RejectTheRequest()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var validator = new AdminSubscriptionAnalyticsTimelineValidator(
            Options.Create(new SubscriptionAnalyticsOptions()),
            timeProvider);

        (await validator.ValidateAsync(new AdminSubscriptionAnalyticsTimelineRequest
        {
            From = new DateOnly(2025, 11, 19),
            To = new DateOnly(2026, 11, 20)
        })).IsValid.ShouldBeFalse();
        (await validator.ValidateAsync(new AdminSubscriptionAnalyticsTimelineRequest
        {
            OwnerType = "Partner"
        })).IsValid.ShouldBeFalse();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
