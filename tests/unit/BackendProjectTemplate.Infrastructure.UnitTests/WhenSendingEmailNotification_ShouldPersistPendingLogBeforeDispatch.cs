using BackendProjectTemplate.Contracts.Commands.Notifications;
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

public sealed class WhenSendingEmailNotification_ShouldPersistPendingLogBeforeDispatch
{
    [Fact]
    public async Task Verify()
    {
        var tenantId = Guid.CreateVersion7();
        var providerRepository = Substitute.For<IReadRepository<EmailProvider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var tenantRepository = Substitute.For<IReadRepository<Tenant>>();
        var logRepository = Substitute.For<IRepository<EmailNotificationLog>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var now = DateTimeOffset.UtcNow;
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(now);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        Guid loggedMessageId = Guid.Empty;
        string loggedTo = string.Empty;
        string? loggedCc = null;
        string? loggedBcc = null;
        bool loggedIsSent = true;
        string? loggedFailureReason = "placeholder";
        Guid loggedTenantId = Guid.Empty;
        Guid loggedCountryId = Guid.Empty;
        NotificationType loggedNotificationType = default;
        Dictionary<string, string> loggedNotificationContent = [];
        logRepository
            .When(repository => repository.AddAsync(Arg.Any<EmailNotificationLog>(), Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                var log = callInfo.Arg<EmailNotificationLog>();
                loggedMessageId = log.MessageId;
                loggedTenantId = log.TenantId;
                loggedCountryId = log.CountryId;
                loggedNotificationType = log.NotificationType;
                loggedNotificationContent = new Dictionary<string, string>(log.NotificationContent);
                loggedTo = log.To;
                loggedCc = log.Cc;
                loggedBcc = log.Bcc;
                loggedIsSent = log.IsSent;
                loggedFailureReason = log.FailureReason;
            });

        var templateRoot = Path.Combine(Path.GetTempPath(), $"email-templates-{Guid.CreateVersion7():N}");
        var tenantTemplateDirectory = Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "NotificationTypes");
        Directory.CreateDirectory(tenantTemplateDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "BaseTemplate.html"),
            "<html><body>{{:BodyHtml:}}</body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(tenantTemplateDirectory, "SignInSuccessful.html"),
            "IP Address: {{:IpAddress:}}");

        var options = Options.Create(new EmailNotificationsOptions
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        });
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
                    ["OtpCode"] = "123456",
                    ["Password"] = "P@ssword!123"
                },
                Cc: ["cc-one@test.local", "cc-two@test.local"],
                Bcc: ["bcc-one@test.local"]));

        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveEmailProviderSpecification>(), Arg.Any<CancellationToken>())
            .Returns(EmailProvider.Create("Logging", "logging", true, now));
        templateRepository.FirstOrDefaultAsync(Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(
                NotificationType.SignInSuccessful,
                "Sign-in successful notification",
                "Subject {{:IpAddress:}}",
                "SignInSuccessful.html",
                now));
        tenantRepository.FirstOrDefaultAsync(Arg.Any<TenantByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Tenant.Create(tenantId, "Moveaex", "moveaex", now));
        transportProvider.ProviderKey.Returns("logging");
        hostEnvironment.ContentRootPath.Returns(templateRoot);

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            tenantRepository,
            logRepository,
            [transportProvider],
            hostEnvironment,
            options,
            unitOfWork,
            timeProvider);

        await sut.SendAsync(command, CancellationToken.None);

        await logRepository.Received(1).AddAsync(Arg.Any<EmailNotificationLog>(), Arg.Any<CancellationToken>());
        loggedMessageId.ShouldBe(command.MessageId);
        loggedTenantId.ShouldBe(command.TenantId);
        loggedCountryId.ShouldBe(command.CountryId);
        loggedNotificationType.ShouldBe(command.NotificationType);
        loggedTo.ShouldBe(((EmailNotificationContent)command.NotificationContent).To);
        loggedCc.ShouldBe("cc-one@test.local,cc-two@test.local");
        loggedBcc.ShouldBe("bcc-one@test.local");
        loggedIsSent.ShouldBeFalse();
        loggedFailureReason.ShouldBeNull();
        loggedNotificationContent["IpAddress"].ShouldBe("127.0.0.1");
        loggedNotificationContent["OtpCode"].ShouldBe("***");
        loggedNotificationContent["Password"].ShouldBe("***");
        loggedNotificationContent.Values.ShouldNotContain("123456");
        loggedNotificationContent.Values.ShouldNotContain("P@ssword!123");
        logRepository.Received(1).Update(Arg.Is<EmailNotificationLog>(log => log.IsSent && log.FailureReason == null));
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
