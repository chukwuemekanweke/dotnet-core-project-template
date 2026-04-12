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
            " SignInSuccessful.html ",
            utcNow);

        template.NotificationType.ShouldBe(NotificationType.SignInSuccessful);
        template.Description.ShouldBe("Sign-in successful notification");
        template.Subject.ShouldBe("Successful sign-in");
        template.TemplateFileName.ShouldBe("SignInSuccessful.html");
        template.CreatedAtUtc.ShouldBe(default);
        template.UpdatedAtUtc.ShouldBe(default);
    }
}
