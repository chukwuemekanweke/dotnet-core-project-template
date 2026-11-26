namespace BackendProjectTemplate.WebAPI.Features.Providers;

/// <summary>The optional reason is recorded in the administrative audit journal.</summary>
public sealed record ActivateProviderRequest(string ProviderType, string ProviderKey, string? Reason = null);
