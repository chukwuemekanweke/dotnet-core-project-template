using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class WhenSendingEmailNotificationWithExistingSentLog_Should
{
    [Fact]
    public async Task SkipDispatch()
    {
        var providerRepository = Substitute.For<IReadRepository<Provider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var tenantRepository = Substitute.For<IReadRepository<Tenant>>();
        var logRepository = Substitute.For<IRepository<EmailNotificationLog>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = Substitute.For<TimeProvider>();
        var now = DateTimeOffset.UtcNow;
        var command = CreateCommand();
        var existingLog = EmailNotificationLog.Create(
            command.MessageId,
            command.TenantId,
            command.CountryId,
            command.NotificationType,
            [],
            ((EmailNotificationContent)command.NotificationContent).To,
            null,
            null);
        existingLog.MarkSent(now);

        logRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationLogByMessageIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(existingLog);

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            tenantRepository,
            logRepository,
            [transportProvider],
            hostEnvironment,
            Options.Create(CreateOptions()),
            unitOfWork,
            timeProvider);

        await sut.SendAsync(command, CancellationToken.None);

        await logRepository.DidNotReceive().AddAsync(Arg.Any<EmailNotificationLog>(), Arg.Any<CancellationToken>());
        logRepository.DidNotReceive().Update(Arg.Any<EmailNotificationLog>());
        await transportProvider.DidNotReceive().SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static SendNotificationCommand CreateCommand() =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1"
                }));

    private static EmailNotificationsOptions CreateOptions() =>
        new()
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        };
}
