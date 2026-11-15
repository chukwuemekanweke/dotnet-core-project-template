using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

public sealed record StepUpMfaIntendedRequest(
    string Method,
    string Path,
    IReadOnlyDictionary<string, string[]>? Query,
    JsonElement? Body);
