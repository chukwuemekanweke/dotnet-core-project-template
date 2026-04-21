using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingEmailNotificationCommand_Should
{
    [Fact]
    public async Task SendThroughEmailNotificationService()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var emailNotificationService = Substitute.For<IEmailNotificationService>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var command = new SendNotificationCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                ConsumerTestData.Email(),
                new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1",
                    ["UserAgent"] = "Test Agent"
                }));

        await new SendNotificationHandler(customTelemetryContext, currentActorAccessor, messageContext, emailNotificationService)
            .HandleAsync(command, CancellationToken.None);

        await emailNotificationService.Received(1).SendAsync(command, Arg.Any<CancellationToken>());
    }
}

