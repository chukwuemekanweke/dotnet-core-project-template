using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.LinkGoogleAccount;

public sealed record LinkGoogleAccountCommand(
    string FlowToken,
    string Password,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
