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
        DateTimeOffset enqueuedAtUtc)
    {
        MessageId = messageId;
        Kind = kind;
        Type = type;
        Payload = payload;
        EnqueuedAtUtc = enqueuedAtUtc;
        SetAuditDates(enqueuedAtUtc);
    }

    public Guid MessageId { get; private set; }
    public OutboxMessageKind Kind { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset EnqueuedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    public static OutboxMessage CreateEvent(
        Guid messageId,
        string type,
        string payload,
        DateTimeOffset occurredAtUtc) =>
        new(messageId, OutboxMessageKind.Event, type, payload, occurredAtUtc);

    public static OutboxMessage CreateCommand(
        Guid messageId,
        string type,
        string payload,
        DateTimeOffset requestedAtUtc) =>
        new(messageId, OutboxMessageKind.Command, type, payload, requestedAtUtc);

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
