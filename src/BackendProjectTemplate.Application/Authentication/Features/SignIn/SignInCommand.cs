namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record SignInCommand(
    string Email,
    string Password,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
