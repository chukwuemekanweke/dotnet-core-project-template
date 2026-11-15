namespace BackendProjectTemplate.Domain.Subscriptions;

/// <summary>
/// PostgreSQL <c>timestamptz</c> stores microseconds while <see cref="DateTimeOffset"/> carries 100ns ticks.
/// Lifecycle boundaries are compared for exact equality by state-checked scheduled commands (activation,
/// renewal, expiry), so they are normalized to UTC microsecond precision before being stored or scheduled.
/// </summary>
public static class SubscriptionTimestamps
{
    private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

    public static DateTimeOffset Normalize(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.UtcTicks - (utc.UtcTicks % TicksPerMicrosecond), TimeSpan.Zero);
    }
}
