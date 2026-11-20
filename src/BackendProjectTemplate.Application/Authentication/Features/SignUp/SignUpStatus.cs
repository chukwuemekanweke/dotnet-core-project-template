namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

public enum SignUpStatus
{
    Accepted = 1,
    DuplicateEmail = 2,
    ValidationFailed = 3,
    CountryMismatch = 4
}
