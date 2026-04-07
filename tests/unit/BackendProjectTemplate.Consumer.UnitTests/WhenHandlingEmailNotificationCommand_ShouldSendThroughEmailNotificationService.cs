using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Observability;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingEmailNotificationCommand_ShouldSendThroughEmailNotificationService
{
    [Fact]
    public async Task Verify()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var emailNotificationService = Substitute.For<IEmailNotificationService>();
        var command = new SendNotificationCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                ConsumerTestData.Email(),
                ["A sign-in to your account was successful."]));

        await new SendNotificationHandler(customTelemetryContext, emailNotificationService)
            .HandleAsync(command, CancellationToken.None);

        await emailNotificationService.Received(1).SendAsync(command, Arg.Any<CancellationToken>());
    }
}
