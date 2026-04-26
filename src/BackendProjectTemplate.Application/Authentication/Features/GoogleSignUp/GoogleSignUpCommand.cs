namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record GoogleSignUpCommand(
    string IdToken,
    Guid CountryId,
    string FirstName,
    string LastName,
    ActorContext ActorContext);
