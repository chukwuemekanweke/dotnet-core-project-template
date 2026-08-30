using BackendProjectTemplate.Domain.Common.Localization;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Registrations;

public sealed record GoogleSignUpRequest(
    string IdToken,
    Guid CountryId,
    string FirstName,
    string LastName,
    string Language = SupportedLanguages.English);
