using BackendProjectTemplate.Domain.Common.Analytics;

namespace BackendProjectTemplate.Domain.Payments.ReadModels;

public interface IPaymentAnalyticsReadModelRepository
{
    Task<PaymentAnalyticsSummaryReadModel> GetSummaryAsync(PaymentAnalyticsFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentAnalyticsTimelineRow>> GetTimelineAsync(PaymentAnalyticsFilter filter, Interval interval, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentAnalyticsStatusRow>> GetStatusesAsync(PaymentAnalyticsFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentAnalyticsProviderRow>> GetProvidersAsync(PaymentAnalyticsFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentAnalyticsIntentRow>> GetIntentsAsync(PaymentAnalyticsFilter filter, CancellationToken cancellationToken);
}
