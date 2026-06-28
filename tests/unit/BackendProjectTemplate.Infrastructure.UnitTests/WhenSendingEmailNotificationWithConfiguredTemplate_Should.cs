using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Providers.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;
using BackendProjectTemplate.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenSendingEmailNotificationWithConfiguredTemplate_Should
{
    [Fact]
    public async Task RenderAndDispatchEmail()
    {
        var tenantId = Guid.CreateVersion7();
        var templateRoot = Path.Combine(Path.GetTempPath(), $"email-templates-{Guid.CreateVersion7():N}");
        var tenantTemplateDirectory = Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "NotificationTypes");
        Directory.CreateDirectory(tenantTemplateDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "BaseTemplate.html"),
            "<html><body><header>{{:Subject:}}</header>{{:BodyHtml:}}<footer>Tenant Footer</footer></body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(tenantTemplateDirectory, "SignInSuccessful.html"),
            "A sign-in to your account was successful.\nIP Address: {{:IpAddress:}}\nUser Agent: {{:UserAgent:}}");

        var providerRepository = Substitute.For<IReadRepository<Provider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var tenantRepository = Substitute.For<IReadRepository<Tenant>>();
        var emailNotificationLogRepository = Substitute.For<IRepository<EmailNotificationLog>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var options = Options.Create(new EmailNotificationsOptions
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        });
        var now = DateTimeOffset.UtcNow;
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(now);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var command = new SendNotificationCommand(
            tenantId,
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

        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveProviderByTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true));
        templateRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(NotificationType.SignInSuccessful, "Sign-in successful notification", "Successful sign-in from {{:IpAddress:}}", "SignInSuccessful.html"));
        tenantRepository.FirstOrDefaultAsync(
                Arg.Any<TenantByIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(Tenant.Create(tenantId, "Moveaex", "moveaex"));
        transportProvider.ProviderKey.Returns("mailtrap");
        transportProvider.SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>())
            .Returns(new EmailTransportSendResult("mailtrap-message-id"));
        hostEnvironment.ContentRootPath.Returns(templateRoot);

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            tenantRepository,
            emailNotificationLogRepository,
            [transportProvider],
            hostEnvironment,
            options,
            unitOfWork,
            timeProvider);

        await sut.SendAsync(command, CancellationToken.None);

        await transportProvider.Received(1).SendAsync(
            Arg.Is<EmailDeliveryMessage>(message =>
                message.Subject == "Successful sign-in from 127.0.0.1" &&
                message.To == ((EmailNotificationContent)command.NotificationContent).To &&
                message.HtmlBody == "<html><body><header>Successful sign-in from 127.0.0.1</header><p>A sign-in to your account was successful.</p><p>IP Address: 127.0.0.1</p><p>User Agent: Test Agent</p><footer>Tenant Footer</footer></body></html>"),
            Arg.Any<CancellationToken>());
    }
}









