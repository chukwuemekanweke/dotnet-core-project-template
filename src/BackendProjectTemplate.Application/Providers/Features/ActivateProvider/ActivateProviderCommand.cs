using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Application.Providers.Features.ActivateProvider;

public sealed record ActivateProviderCommand(ProviderType ProviderType, string ProviderKey);
