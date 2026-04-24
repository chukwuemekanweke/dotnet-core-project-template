namespace BackendProjectTemplate.Domain.Payments.Services;

public sealed record PaymentProviderWebhookParseRequest(
    string RawPayload);
