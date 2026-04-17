using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Common.Messaging;

public sealed class OutboxMessage : Entity
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(
        Guid messageId,
        OutboxMessageKind kind,
        string type,
        string payload,
        string? correlationId,
        string? activityId,
        DateTimeOffset enqueuedAtUtc)
    {
        MessageId = messageId;
        Kind = kind;
        Type = type;
        Payload = payload;
        CorrelationId = correlationId;
        ActivityId = activityId;
        EnqueuedAtUtc = enqueuedAtUtc;
    }

    public Guid MessageId { get; private set; }
    public OutboxMessageKind Kind { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public string? ActivityId { get; private set; }
    public DateTimeOffset EnqueuedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    public static OutboxMessage CreateEvent(
        Guid messageId,
        string type,
        string payload,
        DateTimeOffset occurredAtUtc,
        string? correlationId = null,
        string? activityId = null) =>
        new(messageId, OutboxMessageKind.Event, type, payload, correlationId, activityId, occurredAtUtc);

    public static OutboxMessage CreateCommand(
        Guid messageId,
        string type,
        string payload,
        DateTimeOffset requestedAtUtc,
        string? correlationId = null,
        string? activityId = null) =>
        new(messageId, OutboxMessageKind.Command, type, payload, correlationId, activityId, requestedAtUtc);

    public void MarkAttempt(DateTimeOffset utcNow, string? error = null)
    {
        AttemptCount++;
        LastAttemptAtUtc = utcNow;
        LastError = error;
        Touch(utcNow);
    }

    public void MarkSent(DateTimeOffset utcNow)
    {
        SentAtUtc = utcNow;
        LastAttemptAtUtc = utcNow;
        LastError = null;
        Touch(utcNow);
    }
}
