using BackendProjectTemplate.Application.Payments.Analytics;

namespace BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsSummary;

public sealed class GetAdminPaymentAnalyticsSummaryHandler(PaymentAnalyticsService service)
{
    public Task<PaymentAnalyticsSummaryResponse> HandleAsync(PaymentAnalyticsQuery query, CancellationToken cancellationToken) =>
        service.GetSummaryAsync(query, cancellationToken);
}
