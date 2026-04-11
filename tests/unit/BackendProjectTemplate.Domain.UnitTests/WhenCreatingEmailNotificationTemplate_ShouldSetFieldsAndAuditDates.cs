using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Notifications.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingEmailNotificationTemplate_ShouldSetFieldsAndAuditDates
{
    [Fact]
    public void Verify()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var template = EmailNotificationTemplate.Create(
            NotificationType.SignInSuccessful,
            " Sign-in successful notification ",
            " Successful sign-in ",
            " Sign-in succeeded. ",
            utcNow);

        template.NotificationType.ShouldBe(NotificationType.SignInSuccessful);
        template.Description.ShouldBe("Sign-in successful notification");
        template.Subject.ShouldBe("Successful sign-in");
        template.Body.ShouldBe("Sign-in succeeded.");
        template.CreatedAtUtc.ShouldBe(utcNow);
        template.UpdatedAtUtc.ShouldBe(utcNow);
    }
}
