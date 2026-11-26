using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Providers.Entities;

namespace BackendProjectTemplate.Application.Providers.Features.ActivateProvider;

public sealed record ActivateProviderCommand(
    ProviderType ProviderType,
    string ProviderKey,
    ActorContext ActorContext,
    string? Reason = null);
