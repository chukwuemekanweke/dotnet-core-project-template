namespace BackendProjectTemplate.Application.Authentication.Features.RefreshSession;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record RefreshSessionCommand(
    string RefreshToken,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
