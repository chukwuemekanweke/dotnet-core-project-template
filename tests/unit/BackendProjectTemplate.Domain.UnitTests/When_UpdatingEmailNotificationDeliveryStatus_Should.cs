using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class When_UpdatingEmailNotificationDeliveryStatus_Should
{
    [Fact]
    public void KeepSendFailureSeparateFromDeliveryFailure()
    {
        var log = CreateLog();

        log.MarkFailed("Mailtrap API unavailable");

        log.DeliveryStatus.ShouldBe(EmailDeliveryStatus.Pending);
        log.FailureReason.ShouldBe("Mailtrap API unavailable");
        log.DeliveryFailureReason.ShouldBeNull();
        log.FailedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void RecordBounceAsTerminalOutcome()
    {
        var log = CreateLog();
        var sentAtUtc = DateTimeOffset.Parse("2026-11-14T09:00:00+00:00");
        var bouncedAtUtc = DateTimeOffset.Parse("2026-11-14T09:01:00+00:00");
        log.MarkSent("provider-message-id", sentAtUtc);

        log.MarkBounced(bouncedAtUtc, "Mailbox unavailable");

        log.DeliveryStatus.ShouldBe(EmailDeliveryStatus.Bounced);
        log.SentAtUtc.ShouldBe(sentAtUtc);
        log.FailedAtUtc.ShouldBe(bouncedAtUtc);
        log.LastDeliveryEventAtUtc.ShouldBe(bouncedAtUtc);
        log.DeliveryFailureReason.ShouldBe("Mailbox unavailable");
        log.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void IgnoreLateConflictingTerminalOutcome()
    {
        var log = CreateLog();
        var deliveredAtUtc = DateTimeOffset.Parse("2026-11-14T09:01:00+00:00");
        log.MarkDelivered(deliveredAtUtc);

        log.MarkRejected(deliveredAtUtc.AddMinutes(1), "Suppressed recipient");

        log.DeliveryStatus.ShouldBe(EmailDeliveryStatus.Delivered);
        log.DeliveredAtUtc.ShouldBe(deliveredAtUtc);
        log.LastDeliveryEventAtUtc.ShouldBe(deliveredAtUtc);
        log.DeliveryFailureReason.ShouldBeNull();
    }

    private static EmailNotificationLog CreateLog() =>
        EmailNotificationLog.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            [],
            "ada@example.com",
            null,
            null,
            DateTimeOffset.Parse("2026-11-14T08:59:00+00:00"));
}
