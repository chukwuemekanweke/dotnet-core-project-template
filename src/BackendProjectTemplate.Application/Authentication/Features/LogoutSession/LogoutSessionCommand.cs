namespace BackendProjectTemplate.Application.Authentication.Features.LogoutSession;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record LogoutSessionCommand(
    string TokenId,
    DateTimeOffset ExpiresAtUtc,
    Guid? StakeholderId,
    ActorContext ActorContext);
