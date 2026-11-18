namespace BackendProjectTemplate.Domain.Common.Analytics;

public static class AnalyticsMath
{
    public static decimal Percentage(long numerator, long denominator, int decimalPlaces = 2) =>
        denominator == 0 ? 0m : decimal.Round(numerator * 100m / denominator, decimalPlaces);

    public static decimal Average(decimal total, long count, int decimalPlaces = 2) =>
        count == 0 ? 0m : decimal.Round(total / count, decimalPlaces);
}
