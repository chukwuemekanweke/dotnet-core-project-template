using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Notifications.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class When_MarkingEmailNotificationLogAsDelivered_WhenAlreadyDelivered_Should
{
    [Fact]
    public void KeepOriginalDeliveredAtUtc()
    {
        var enqueuedAtUtc = DateTimeOffset.Parse("2026-05-03T10:00:00+00:00");
        var firstDeliveredAtUtc = DateTimeOffset.Parse("2026-05-03T11:00:00+00:00");
        var secondDeliveredAtUtc = DateTimeOffset.Parse("2026-05-03T12:00:00+00:00");
        var log = EmailNotificationLog.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            [],
            "ada@example.com",
            null,
            null,
            enqueuedAtUtc);

        log.MarkDelivered(firstDeliveredAtUtc, firstDeliveredAtUtc);
        log.MarkDelivered(secondDeliveredAtUtc, secondDeliveredAtUtc);

        log.DeliveredAtUtc.ShouldBe(firstDeliveredAtUtc);
    }
}
