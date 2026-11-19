using BackendProjectTemplate.Application.Payments.Analytics;

namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsStatuses;

public sealed class GetAdminPaymentAnalyticsStatusesHandler(PaymentAnalyticsService service)
{
    public Task<PaymentAnalyticsStatusesResponse> HandleAsync(PaymentAnalyticsQuery query, CancellationToken cancellationToken) =>
        service.GetStatusesAsync(query, cancellationToken);
}
