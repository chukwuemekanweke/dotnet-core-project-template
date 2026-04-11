namespace BackendProjectTemplate.Infrastructure.Notifications;

public sealed class EmailNotificationsOptions
{
    public const string SectionName = "Notifications:Email";

    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public MailtrapOptions Mailtrap { get; init; } = new();

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FromAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(FromName);
    }

    public sealed class MailtrapOptions
    {
        public string ApiToken { get; init; } = string.Empty;
        public bool UseBulkApi { get; init; }
        public long? InboxId { get; init; }

        public void Validate()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ApiToken);
        }
    }
}
