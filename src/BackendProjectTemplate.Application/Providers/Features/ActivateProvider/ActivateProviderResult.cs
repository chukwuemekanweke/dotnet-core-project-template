namespace BackendProjectTemplate.Application.Providers.Features.ActivateProvider;

public sealed record ActivateProviderResult(ActivateProviderStatus Status, string? Error = null);
