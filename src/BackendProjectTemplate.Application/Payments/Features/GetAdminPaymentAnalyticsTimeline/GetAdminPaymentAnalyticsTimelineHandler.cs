using BackendProjectTemplate.Application.Payments.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;

namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsTimeline;

public sealed class GetAdminPaymentAnalyticsTimelineHandler(PaymentAnalyticsService service)
{
    public Task<PaymentAnalyticsTimelineResponse> HandleAsync(
        PaymentAnalyticsQuery query,
        Interval interval,
        CancellationToken cancellationToken) => service.GetTimelineAsync(query, interval, cancellationToken);
}
