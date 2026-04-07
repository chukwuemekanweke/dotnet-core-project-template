namespace BackendProjectTemplate.Infrastructure.Notifications;

public sealed class EmailNotificationsOptions
{
    public const string SectionName = "Notifications:Email";

    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FromAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(FromName);
    }
}
