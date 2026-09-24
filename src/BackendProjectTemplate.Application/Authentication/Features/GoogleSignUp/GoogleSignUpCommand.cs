namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record GoogleSignUpCommand(
    string FlowToken,
    Guid CountryId,
    string FirstName,
    string LastName,
    string IpAddress,
    ActorContext ActorContext,
    string Language,
    string UserAgent = "");
