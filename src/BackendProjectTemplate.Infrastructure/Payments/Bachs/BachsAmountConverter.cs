using System.Globalization;

namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal static class BachsAmountConverter
{
    private static readonly IReadOnlyDictionary<string, int> SupportedCurrencyPrecision =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["NGN"] = 2,
            ["USD"] = 2
        };

    public static string ToMajorUnitString(decimal minorUnitAmount, string currencyCode)
    {
        var precision = GetPrecision(currencyCode);
        if (minorUnitAmount <= 0 || decimal.Truncate(minorUnitAmount) != minorUnitAmount)
        {
            throw new InvalidOperationException("Bachs amount must be a positive whole number of minor currency units.");
        }

        return (minorUnitAmount / Pow10(precision)).ToString($"F{precision}", CultureInfo.InvariantCulture);
    }

    public static decimal ToMinorUnits(string majorUnitAmount, string currencyCode)
    {
        var precision = GetPrecision(currencyCode);
        if (!decimal.TryParse(majorUnitAmount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new InvalidOperationException("Bachs returned an invalid amount.");
        }

        var minorUnits = parsed * Pow10(precision);
        if (decimal.Truncate(minorUnits) != minorUnits)
        {
            throw new InvalidOperationException("Bachs returned an amount with unsupported currency precision.");
        }

        return minorUnits;
    }

    private static int GetPrecision(string currencyCode) =>
        SupportedCurrencyPrecision.TryGetValue(currencyCode.Trim(), out var precision)
            ? precision
            : throw new InvalidOperationException($"Currency '{currencyCode}' is not supported by the Bachs integration.");

    private static decimal Pow10(int precision) => precision switch
    {
        0 => 1m,
        2 => 100m,
        _ => throw new InvalidOperationException($"Currency precision '{precision}' is not supported.")
    };
}
