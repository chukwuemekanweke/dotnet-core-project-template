using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;

public sealed class AdminSubscriptionAnalyticsTimelineValidator
    : AdminSubscriptionAnalyticsValidatorBase<AdminSubscriptionAnalyticsTimelineRequest>
{
    public AdminSubscriptionAnalyticsTimelineValidator(
        IOptions<SubscriptionAnalyticsOptions> options,
        TimeProvider timeProvider) : base(options.Value, timeProvider)
    {
        RuleFor(request => request.Interval)
            .Must(StrictEnumParser.IsValid<Interval>)
            .WithMessage("Interval must be Day, Week, Month or Year.");
    }
}
