namespace BackendProjectTemplate.WebAPI.Features.Authentication.Registrations;

public sealed record GoogleSignUpRequest(
    string IdToken,
    Guid CountryId,
    string FirstName,
    string LastName);
