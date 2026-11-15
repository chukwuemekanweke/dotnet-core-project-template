namespace BackendProjectTemplate.WebAPI.Features.Subscriptions;

/// <summary>
/// Parses enum names case-insensitively while rejecting numeric strings and undefined values, which
/// <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/> would otherwise accept.
/// </summary>
public static class StrictEnumParser
{
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value) &&
            !value.Trim().All(character => char.IsDigit(character) || character is '-' or '+') &&
            Enum.TryParse(value.Trim(), true, out result) &&
            Enum.IsDefined(result);
    }

    public static bool IsValid<TEnum>(string? value)
        where TEnum : struct, Enum =>
        TryParse<TEnum>(value, out _);

    public static bool IsValidOrEmpty<TEnum>(string? value)
        where TEnum : struct, Enum =>
        string.IsNullOrWhiteSpace(value) || TryParse<TEnum>(value, out _);

    public static TEnum? ParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        TryParse<TEnum>(value, out var result) ? result : null;

    public static TEnum Parse<TEnum>(string? value)
        where TEnum : struct, Enum =>
        TryParse<TEnum>(value, out var result)
            ? result
            : throw new ArgumentException($"'{value}' is not a valid {typeof(TEnum).Name}.", nameof(value));
}
