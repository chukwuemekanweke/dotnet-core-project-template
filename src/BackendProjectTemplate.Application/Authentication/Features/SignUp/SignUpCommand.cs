namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

public sealed record SignUpCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName);
