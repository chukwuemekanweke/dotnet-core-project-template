namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderWebhookValidationRequest(string RawPayload)
{
    public string? SignatureHeader { get; init; }
}
