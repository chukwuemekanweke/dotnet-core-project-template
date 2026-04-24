namespace BackendProjectTemplate.Domain.Payments.Services;

public interface IPaymentProviderService
{
    string ProviderKey { get; }
    Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken);
    Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken);
    Task<PaymentProviderWebhookValidationResult> ValidateWebhookAsync(
        PaymentProviderWebhookValidationRequest request,
        CancellationToken cancellationToken);
    Task<PaymentProviderWebhookParseResult> ParseWebhookAsync(
        PaymentProviderWebhookParseRequest request,
        CancellationToken cancellationToken);
}
