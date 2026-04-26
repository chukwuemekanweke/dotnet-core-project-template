namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record GoogleSignInCommand(
    string IdToken,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
