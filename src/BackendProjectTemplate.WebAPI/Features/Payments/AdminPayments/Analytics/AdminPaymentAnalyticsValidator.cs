using BackendProjectTemplate.Application.Payments.Analytics;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments.Analytics;

public sealed class AdminPaymentAnalyticsValidator : AdminPaymentAnalyticsValidatorBase<AdminPaymentAnalyticsRequest>
{
    public AdminPaymentAnalyticsValidator(IOptions<PaymentAnalyticsOptions> options, TimeProvider timeProvider)
        : base(options.Value, timeProvider)
    {
    }
}
