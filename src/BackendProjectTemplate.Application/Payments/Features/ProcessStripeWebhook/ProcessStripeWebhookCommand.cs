namespace BackendProjectTemplate.Application.Payments.Features.ProcessStripeWebhook;

public sealed record ProcessStripeWebhookCommand(string RawPayload);
