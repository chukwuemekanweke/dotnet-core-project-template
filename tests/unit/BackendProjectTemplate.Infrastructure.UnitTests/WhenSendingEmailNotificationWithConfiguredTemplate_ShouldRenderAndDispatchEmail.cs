using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Infrastructure.Notifications;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenSendingEmailNotificationWithConfiguredTemplate_ShouldRenderAndDispatchEmail
{
    [Fact]
    public async Task Verify()
    {
        var providerRepository = Substitute.For<IReadRepository<EmailProvider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var options = Options.Create(new EmailNotificationsOptions
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template"
        });
        var now = DateTimeOffset.UtcNow;
        var command = new SendNotificationCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1",
                    ["UserAgent"] = "Test Agent"
                }));

        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveEmailProviderSpecification>(), Arg.Any<CancellationToken>())
            .Returns(EmailProvider.Create("Logging", "logging", true, now));
        templateRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(
                NotificationType.SignInSuccessful,
                "Sign-in successful notification",
                "Successful sign-in from {{:IpAddress:}}",
                "A sign-in to your account was successful.\nIP Address: {{:IpAddress:}}\nUser Agent: {{:UserAgent:}}",
                now));
        transportProvider.ProviderKey.Returns("logging");

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            [transportProvider],
            options);

        await sut.SendAsync(command, CancellationToken.None);

        await transportProvider.Received(1).SendAsync(
            Arg.Is<EmailDeliveryMessage>(message =>
                message.Subject == "Successful sign-in from 127.0.0.1" &&
                message.To == ((EmailNotificationContent)command.NotificationContent).To &&
                message.Content.SequenceEqual(new[]
                {
                    "A sign-in to your account was successful.",
                    "IP Address: 127.0.0.1",
                    "User Agent: Test Agent"
                })),
            Arg.Any<CancellationToken>());
    }
}
