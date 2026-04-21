using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingResetPassword_Should
{
    [Fact]
    public async Task GenerateOtpAndSendNotification()
    {
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var appUserId = Guid.CreateVersion7();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var email = ConsumerTestData.Email();
        var otp = new TwoFactorOtp(ConsumerTestData.Otp(), DateTimeOffset.UtcNow.AddMinutes(2));

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        twoFactorOtpService.OtpExistsAsync(appUserId, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(false);
        twoFactorOtpService.GenerateOtpAsync(
                appUserId,
                OtpIntent.PasswordReset,
                Arg.Any<CancellationToken>(),
                6,
                false)
            .Returns(otp);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, tenantId, countryId, Guid.CreateVersion7(), firstName, lastName, null, false));

        await new ResetPasswordHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            twoFactorOtpService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork).HandleAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.TenantId == tenantId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.ResetPasswordOtp &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.StakeholderId == stakeholderId &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["FirstName"] == firstName &&
                ((EmailNotificationContent)command.NotificationContent).Content["LastName"] == lastName &&
                ((EmailNotificationContent)command.NotificationContent).Content["OtpCode"] == otp.Code),
            Arg.Any<CancellationToken>());
        await twoFactorOtpService.Received(1).GenerateOtpAsync(
            appUserId,
            OtpIntent.PasswordReset,
            Arg.Any<CancellationToken>(),
            6,
            false);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

