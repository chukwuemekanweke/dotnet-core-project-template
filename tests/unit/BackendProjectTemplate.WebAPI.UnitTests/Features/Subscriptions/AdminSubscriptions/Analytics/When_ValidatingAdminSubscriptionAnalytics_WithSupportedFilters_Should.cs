using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Analytics;

public sealed class When_ValidatingAdminSubscriptionAnalytics_WithSupportedFilters_Should
{
    [Fact]
    public async Task AcceptTheRequest()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var validator = new AdminSubscriptionAnalyticsTimelineValidator(
            Options.Create(new SubscriptionAnalyticsOptions()),
            timeProvider);

        foreach (var interval in new[] { "Day", "Week", "Month", "Year" })
        {
            foreach (var ownerType in new[] { "Tenant", "Stakeholder", null })
            {
                (await validator.ValidateAsync(new AdminSubscriptionAnalyticsTimelineRequest
                {
                    From = new DateOnly(2025, 11, 20),
                    To = new DateOnly(2026, 11, 20),
                    Interval = interval,
                    OwnerType = ownerType,
                    PlanId = Guid.CreateVersion7(),
                    TenantId = Guid.CreateVersion7(),
                    CurrencyId = Guid.CreateVersion7()
                })).IsValid.ShouldBeTrue($"{interval}/{ownerType}");
            }
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
