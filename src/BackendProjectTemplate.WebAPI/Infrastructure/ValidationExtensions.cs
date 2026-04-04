using FluentValidation.Results;

namespace BackendProjectTemplate.WebAPI.Infrastructure;

public static class ValidationExtensions
{
    public static Dictionary<string, string[]> ToValidationDictionary(this ValidationResult validationResult) =>
        validationResult.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
}
