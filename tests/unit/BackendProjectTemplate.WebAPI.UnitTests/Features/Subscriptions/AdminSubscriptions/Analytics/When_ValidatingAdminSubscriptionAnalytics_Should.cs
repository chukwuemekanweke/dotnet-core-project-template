using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Reflection;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Analytics;

public sealed class When_ValidatingAdminSubscriptionAnalytics_Should
{
    [Fact]
    public async Task RejectInvalidRangesIntervalsOwnersAndIdentifiers()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new SubscriptionAnalyticsOptions());
        var validator = new AdminSubscriptionAnalyticsValidator(options, timeProvider);
        var timelineValidator = new AdminSubscriptionAnalyticsTimelineValidator(options, timeProvider);

        (await validator.ValidateAsync(new AdminSubscriptionAnalyticsRequest
        {
            From = new DateOnly(2026, 11, 21),
            To = new DateOnly(2026, 11, 20),
            PlanId = Guid.Empty,
            OwnerType = "Wallet"
        })).IsValid.ShouldBeFalse();
        (await timelineValidator.ValidateAsync(new AdminSubscriptionAnalyticsTimelineRequest
        {
            Interval = "Quarter"
        })).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task AcceptSupportedFiltersAndRejectRangesBeyondTheConfiguredMaximum()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new SubscriptionAnalyticsOptions());
        var timelineValidator = new AdminSubscriptionAnalyticsTimelineValidator(options, timeProvider);

        foreach (var interval in new[] { "Day", "Week", "Month", "Year" })
        {
            foreach (var ownerType in new[] { "Tenant", "Stakeholder", null })
            {
                (await timelineValidator.ValidateAsync(new AdminSubscriptionAnalyticsTimelineRequest
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

        (await timelineValidator.ValidateAsync(new AdminSubscriptionAnalyticsTimelineRequest
        {
            From = new DateOnly(2025, 11, 19),
            To = new DateOnly(2026, 11, 20)
        })).IsValid.ShouldBeFalse();
        (await timelineValidator.ValidateAsync(new AdminSubscriptionAnalyticsTimelineRequest
        {
            OwnerType = "Partner"
        })).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RequireDedicatedSubscriptionAnalyticsPermission()
    {
        typeof(AdminSubscriptionAnalyticsController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(PermissionPolicyNames.SubscriptionAnalyticsRead);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
