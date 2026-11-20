namespace BackendProjectTemplate.WebAPI.Features.Analytics;

internal static class AdministrativeAnalyticsValidation
{
    public static bool IsValidRange(
        DateOnly? fromValue,
        DateOnly? toValue,
        int defaultRangeDays,
        int maximumRangeDays,
        DateOnly today)
    {
        var to = toValue ?? today;
        if (to > today || to == DateOnly.MaxValue)
        {
            return false;
        }

        DateOnly from;
        try
        {
            from = fromValue ?? to.AddDays(-(defaultRangeDays - 1));
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        if (from > to)
        {
            return false;
        }

        var dayCount = to.DayNumber - from.DayNumber + 1;
        return dayCount <= maximumRangeDays && from.DayNumber >= dayCount;
    }

    public static bool IsNullOrNonEmpty(Guid? value) => !value.HasValue || value.Value != Guid.Empty;
}
