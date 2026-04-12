using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;
using BackendProjectTemplate.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
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
        var templateRoot = Path.Combine(Path.GetTempPath(), $"email-templates-{Guid.CreateVersion7():N}");
        var tenantTemplateDirectory = Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "NotificationTypes");
        Directory.CreateDirectory(tenantTemplateDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "BaseTemplate.html"),
            "<html><body>{{:BodyHtml:}}</body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(tenantTemplateDirectory, "AccountLocked.html"),
            "Locked Until: {{:LockoutEndUtc:}}");

        var providerRepository = Substitute.For<IReadRepository<EmailProvider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var tenantRepository = Substitute.For<IReadRepository<Tenant>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        var options = Options.Create(new EmailNotificationsOptions
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
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
                "AccountLocked.html",
                now));
        tenantRepository.FirstOrDefaultAsync(
                Arg.Any<TenantByIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(Tenant.Create(
                tenantId,
                "Moveaex",
                "moveaex",
                now));
        transportProvider.ProviderKey.Returns("logging");
        hostEnvironment.ContentRootPath.Returns(templateRoot);

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            tenantRepository,
            [transportProvider],
            hostEnvironment,
            options);

        var exception = await Should.ThrowAsync<NotificationConfigurationException>(() =>
            sut.SendAsync(command, CancellationToken.None));

        exception.Message.ShouldBe(
            "The email body template for notification type 'AccountLocked' requires replacement key 'LockoutEndUtc'.");
        await transportProvider.DidNotReceive().SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>());
    }
}
