namespace BackendProjectTemplate.Domain.Payments.Services;

public interface ICredoWebhookSignatureValidator
{
    Task<PaymentProviderWebhookValidationResult> ValidateAsync(
        CredoWebhookSignatureValidationRequest request,
        CancellationToken cancellationToken);
}
