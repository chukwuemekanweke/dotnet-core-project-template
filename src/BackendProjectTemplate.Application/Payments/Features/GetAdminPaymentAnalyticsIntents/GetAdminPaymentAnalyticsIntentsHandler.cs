using BackendProjectTemplate.Application.Payments.Analytics;

namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsIntents;

public sealed class GetAdminPaymentAnalyticsIntentsHandler(PaymentAnalyticsService service)
{
    public Task<PaymentAnalyticsIntentsResponse> HandleAsync(PaymentAnalyticsQuery query, CancellationToken cancellationToken) =>
        service.GetIntentsAsync(query, cancellationToken);
}
