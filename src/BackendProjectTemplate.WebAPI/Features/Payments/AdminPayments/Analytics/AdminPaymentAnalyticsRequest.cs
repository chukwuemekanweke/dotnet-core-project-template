namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments.Analytics;

public class AdminPaymentAnalyticsRequest
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public Guid? PaymentProviderId { get; init; }
    public Guid? CurrencyId { get; init; }
    public string? PaymentIntent { get; init; }
    public string? PaymentStatus { get; init; }
    public Guid? TenantId { get; init; }
}
