using BackendProjectTemplate.WebAPI.Infrastructure;
using FluentValidation.Results;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests;

public sealed class ValidationExtensionsTests
{
    [Fact]
    public void ToValidationDictionary_GroupsMessagesByProperty()
    {
        var validationResult = new ValidationResult([
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("Email", "Email must be valid."),
            new ValidationFailure("Password", "Password is required.")
        ]);

        var dictionary = validationResult.ToValidationDictionary();

        dictionary["Email"].ShouldBe(["Email is required.", "Email must be valid."]);
        dictionary["Password"].ShouldBe(["Password is required."]);
    }
}
