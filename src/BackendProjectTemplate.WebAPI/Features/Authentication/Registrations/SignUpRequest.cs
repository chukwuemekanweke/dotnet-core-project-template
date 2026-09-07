using BackendProjectTemplate.Domain.Common.Localization;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.Registrations;

public sealed record SignUpRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    Guid CountryId,
    string FirstName,
    string LastName,
    string Language = SupportedLanguages.English);
