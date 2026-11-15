namespace BackendProjectTemplate.Contracts.Events;

public sealed record KycWebhookReceived : BaseEvent
{
    public Guid WebhookInboxId { get; init; }
    public string RequestId { get; init; } = string.Empty;
}
