namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments.Analytics;

public sealed class AdminPaymentAnalyticsTimelineRequest : AdminPaymentAnalyticsRequest
{
    public string Interval { get; init; } = "Day";
}
