namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;

public sealed record GoogleSignUpCommand(
    string IdToken,
    Guid CountryId,
    string FirstName,
    string LastName);
