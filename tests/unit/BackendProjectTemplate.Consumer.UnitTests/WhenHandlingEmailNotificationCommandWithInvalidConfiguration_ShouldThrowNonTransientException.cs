using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingEmailNotificationCommandWithInvalidConfiguration_ShouldThrowNonTransientException
{
    [Fact]
    public async Task Verify()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var emailNotificationService = Substitute.For<IEmailNotificationService>();
        var command = new SendNotificationCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.AccountLocked,
            NotificationMedium.Email,
            new EmailNotificationContent(
                ConsumerTestData.Email(),
                new Dictionary<string, string>
                {
                    ["LockedUntilUtc"] = "2026-04-11T00:00:00.0000000+00:00"
                }));

        emailNotificationService
            .SendAsync(command, Arg.Any<CancellationToken>())
            .Returns(_ => throw new NotificationConfigurationException("No email provider is configured."));

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            new SendNotificationHandler(customTelemetryContext, emailNotificationService)
                .HandleAsync(command, CancellationToken.None));

        exception.Message.ShouldBe("No email provider is configured.");
    }
}
