using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.SafeHaven;

public sealed record SafeHavenWebhookRequest(string Type, JsonElement Data);
