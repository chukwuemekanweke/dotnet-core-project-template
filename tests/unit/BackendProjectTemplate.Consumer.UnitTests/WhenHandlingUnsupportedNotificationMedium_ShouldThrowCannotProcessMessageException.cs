using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUnsupportedNotificationMedium_ShouldThrowCannotProcessMessageException
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
            NotificationMedium.Sms,
            new SmsNotificationContent(
                "+234",
                ConsumerTestData.PhoneNumber(),
                ["A sign-in to your account was successful."]));

        var exception = await Should.ThrowAsync<FailedToProcessMessageException>(() =>
            new SendNotificationHandler(customTelemetryContext, emailNotificationService)
                .HandleAsync(command, CancellationToken.None));

        exception.Message.ShouldBe("Notification medium 'Sms' is not supported.");
        await emailNotificationService.DidNotReceive().SendAsync(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>());
    }
}
