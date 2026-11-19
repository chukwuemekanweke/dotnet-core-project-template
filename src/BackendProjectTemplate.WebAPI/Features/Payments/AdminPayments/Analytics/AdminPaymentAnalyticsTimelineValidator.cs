using BackendProjectTemplate.Application.Payments.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments.Analytics;

public sealed class AdminPaymentAnalyticsTimelineValidator
    : AdminPaymentAnalyticsValidatorBase<AdminPaymentAnalyticsTimelineRequest>
{
    public AdminPaymentAnalyticsTimelineValidator(IOptions<PaymentAnalyticsOptions> options, TimeProvider timeProvider)
        : base(options.Value, timeProvider)
    {
        RuleFor(request => request.Interval)
            .Must(StrictEnumParser.IsValid<Interval>)
            .WithMessage("Interval must be Day, Week, Month or Year.");
    }
}
