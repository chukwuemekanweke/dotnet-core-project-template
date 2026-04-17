using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingResetPasswordWithActiveOtp_ShouldNotSendNotification
{
    [Fact]
    public async Task Verify()
    {
        var passwordResetOtpService = Substitute.For<IPasswordResetOtpService>();
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
        var email = ConsumerTestData.Email();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        passwordResetOtpService.GetActiveAsync(appUserId, Arg.Any<CancellationToken>())
            .Returns(new PasswordResetOtp(ConsumerTestData.Otp(), DateTimeOffset.UtcNow.AddMinutes(2)));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, tenantId, countryId, Guid.CreateVersion7(), ConsumerTestData.FirstName(), ConsumerTestData.LastName(), null, false));

        await new ResetPasswordHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            passwordResetOtpService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork).HandleAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await passwordResetOtpService.DidNotReceive().GenerateAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await commandSender.DidNotReceive().SendAsync(Arg.Any<Contracts.Commands.Notifications.SendNotificationCommand>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
