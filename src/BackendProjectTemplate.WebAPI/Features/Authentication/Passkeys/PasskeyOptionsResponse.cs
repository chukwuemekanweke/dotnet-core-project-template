using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;

public sealed record PasskeyOptionsResponse(string FlowToken, JsonElement PublicKey, DateTimeOffset ExpiresAtUtc);
