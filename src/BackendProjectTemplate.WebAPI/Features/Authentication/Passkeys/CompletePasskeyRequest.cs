using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;

public sealed record CompletePasskeyRequest(string FlowToken, JsonElement Credential);
