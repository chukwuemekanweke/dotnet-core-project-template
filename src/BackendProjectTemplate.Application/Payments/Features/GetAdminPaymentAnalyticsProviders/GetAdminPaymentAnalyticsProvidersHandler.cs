using BackendProjectTemplate.Application.Payments.Analytics;

namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsProviders;

public sealed class GetAdminPaymentAnalyticsProvidersHandler(PaymentAnalyticsService service)
{
    public Task<PaymentAnalyticsProvidersResponse> HandleAsync(PaymentAnalyticsQuery query, CancellationToken cancellationToken) =>
        service.GetProvidersAsync(query, cancellationToken);
}
