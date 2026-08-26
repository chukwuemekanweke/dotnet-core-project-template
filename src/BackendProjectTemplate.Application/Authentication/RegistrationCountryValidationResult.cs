namespace BackendProjectTemplate.Application.Authentication;

public enum RegistrationCountryValidationResult
{
    Match = 1,
    Mismatch = 2,
    CountryNotFound = 3,
    Unavailable = 4
}
