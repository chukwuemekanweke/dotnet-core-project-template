using Humanizer;
using System.Globalization;

namespace BackendProjectTemplate.Consumer.Authentication;

public static class NotificationDateTimeFormatter
{
    public static string Format(DateTimeOffset utcDateTime, DateTimeOffset now)
    {
        var formattedDateTime = utcDateTime.UtcDateTime.ToString(
            "dddd, MMMM d, yyyy 'at' h:mm tt 'UTC'",
            CultureInfo.InvariantCulture);
        var relativeDateTime = utcDateTime.UtcDateTime.Humanize(dateToCompareAgainst: now.UtcDateTime);

        return $"{formattedDateTime} ({relativeDateTime})";
    }
}
