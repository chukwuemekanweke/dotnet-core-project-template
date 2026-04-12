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

public sealed class WhenSendingEmailNotificationWithMissingReplacementKey_ShouldThrowNotificationConfigurationException
{
    [Fact]
    public async Task Verify()
    {
        var tenantId = Guid.CreateVersion7();
        var providerRepository = Substitute.For<IReadRepository<EmailProvider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var tenantBaseTemplateRepository = Substitute.For<IReadRepository<TenantEmailBaseTemplate>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var options = Options.Create(new EmailNotificationsOptions
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template"
        });
        var now = DateTimeOffset.UtcNow;
        var command = new SendNotificationCommand(
            tenantId,
            Guid.CreateVersion7(),
            NotificationType.AccountLocked,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["LockedUntilUtc"] = "2026-04-11T00:00:00.0000000+00:00"
                }));

        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveEmailProviderSpecification>(), Arg.Any<CancellationToken>())
            .Returns(EmailProvider.Create("Logging", "logging", true, now));
        templateRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(
                NotificationType.AccountLocked,
                "Account locked notification",
                "Account locked",
                "Locked Until: {{:LockoutEndUtc:}}",
                now));
        tenantBaseTemplateRepository.FirstOrDefaultAsync(
                Arg.Any<TenantEmailBaseTemplateByTenantIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(TenantEmailBaseTemplate.Create(
                tenantId,
                "Tenant brand",
                "<html><body>{{:BodyHtml:}}</body></html>",
                now));
        transportProvider.ProviderKey.Returns("logging");

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            tenantBaseTemplateRepository,
            [transportProvider],
            options);

        var exception = await Should.ThrowAsync<NotificationConfigurationException>(() =>
            sut.SendAsync(command, CancellationToken.None));

        exception.Message.ShouldBe(
            "The email body template for notification type 'AccountLocked' requires replacement key 'LockoutEndUtc'.");
        await transportProvider.DidNotReceive().SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>());
    }
}
