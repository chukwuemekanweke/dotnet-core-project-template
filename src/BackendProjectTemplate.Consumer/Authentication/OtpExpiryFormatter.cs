using Humanizer;
using System.Globalization;

namespace BackendProjectTemplate.Consumer.Authentication;

public static class OtpExpiryFormatter
{
    public static string Format(DateTimeOffset expiresAtUtc, DateTimeOffset now)
    {
        var formattedExpiry = expiresAtUtc.UtcDateTime.ToString(
            "dddd, MMMM d, yyyy 'at' h:mm tt 'UTC'",
            CultureInfo.InvariantCulture);
        var relativeExpiry = expiresAtUtc.UtcDateTime.Humanize(dateToCompareAgainst: now.UtcDateTime);

        return $"{formattedExpiry} ({relativeExpiry})";
    }
}
