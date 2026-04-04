using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

internal static class IdentityResultExtensions
{
    public static IReadOnlyDictionary<string, string[]> ToValidationDictionary(this IdentityResult result) =>
        result.Errors
            .GroupBy(GetPropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).Distinct().ToArray());

    private static string GetPropertyName(IdentityError error) =>
        error.Code switch
        {
            nameof(IdentityErrorDescriber.DuplicateEmail) => nameof(SignUpRequest.Email),
            nameof(IdentityErrorDescriber.DuplicateUserName) => nameof(SignUpRequest.Email),
            nameof(IdentityErrorDescriber.InvalidEmail) => nameof(SignUpRequest.Email),
            nameof(IdentityErrorDescriber.InvalidUserName) => nameof(SignUpRequest.Email),
            nameof(IdentityErrorDescriber.PasswordMismatch) => nameof(SignUpRequest.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresDigit) => nameof(SignUpRequest.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresLower) => nameof(SignUpRequest.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => nameof(SignUpRequest.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresUpper) => nameof(SignUpRequest.Password),
            nameof(IdentityErrorDescriber.PasswordTooShort) => nameof(SignUpRequest.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => nameof(SignUpRequest.Password),
            _ => string.Empty
        };
}
