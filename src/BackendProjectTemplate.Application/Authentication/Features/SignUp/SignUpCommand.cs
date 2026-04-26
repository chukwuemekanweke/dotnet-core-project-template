namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record SignUpCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    Guid CountryId,
    string FirstName,
    string LastName,
    ActorContext ActorContext);
