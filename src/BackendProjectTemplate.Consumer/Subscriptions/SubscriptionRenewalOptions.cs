namespace BackendProjectTemplate.Consumer.Subscriptions;

public sealed class SubscriptionRenewalOptions
{
    public const string SectionName = "Subscriptions:Renewal";

    public int MaximumRetryAttempts { get; set; } = 3;
    public TimeSpan[] RetryBackoff { get; set; } = [TimeSpan.FromDays(1), TimeSpan.FromDays(3), TimeSpan.FromDays(5)];
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan[] ReminderSchedule { get; set; } = [TimeSpan.FromDays(2), TimeSpan.FromDays(6)];
    public TimeSpan MaximumPaymentResolutionDelay { get; set; } = TimeSpan.FromHours(24);
}
