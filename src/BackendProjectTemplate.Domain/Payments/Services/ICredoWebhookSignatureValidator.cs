namespace BackendProjectTemplate.Domain.Payments.Services;

public interface ICredoWebhookSignatureValidator
{
    Task<PaymentProviderWebhookValidationResult> ValidateAsync(
        PaymentProviderWebhookValidationRequest request,
        CancellationToken cancellationToken);
}
