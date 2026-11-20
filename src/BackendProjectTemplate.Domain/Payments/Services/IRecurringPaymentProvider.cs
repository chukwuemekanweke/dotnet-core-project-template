namespace BackendProjectTemplate.Domain.Payments.Services;

public interface IRecurringPaymentProvider
{
    string ProviderKey { get; }

    Task<RecurringChargeResult> ChargeAsync(RecurringChargeRequest request, CancellationToken cancellationToken);

    Task<RecurringChargeResult> GetChargeAsync(RecurringChargeVerificationRequest request, CancellationToken cancellationToken);
}

public sealed record RecurringChargeRequest(
    string MerchantReference,
    decimal Amount,
    string CurrencyCode,
    string ProviderCustomerReference,
    string PaymentMethodReference,
    string IdempotencyKey,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record RecurringChargeVerificationRequest(
    string ProviderReference,
    string MerchantReference,
    decimal Amount,
    string CurrencyCode);

public sealed record RecurringChargeResult(
    RecurringChargeStatus Status,
    string? ProviderReference,
    string? FailureCode,
    string? FailureReason);
