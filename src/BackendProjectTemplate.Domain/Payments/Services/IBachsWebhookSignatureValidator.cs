namespace BackendProjectTemplate.Domain.Payments.Services;

public interface IBachsWebhookSignatureValidator
{
    Task<PaymentProviderWebhookValidationResult> ValidateAsync(
        BachsWebhookSignatureValidationRequest request,
        CancellationToken cancellationToken);
}
