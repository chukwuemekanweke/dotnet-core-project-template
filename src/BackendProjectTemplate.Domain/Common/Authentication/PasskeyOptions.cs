namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record PasskeyOptions(string FlowToken, string OptionsJson, DateTimeOffset ExpiresAtUtc);
