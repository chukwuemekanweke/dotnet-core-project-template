using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using UserCreatedEvent = BackendProjectTemplate.Contracts.Events.UserCreated;

namespace BackendProjectTemplate.Consumer.UnitTests.Authentication.UserCreated;

public sealed class When_HandlingUserCreated_WithActiveOtp_Should
{
    [Fact]
    public async Task CaptureSkippedEventWithReason()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var user = AppUser.Create(ConsumerTestData.Email());
        var stakeholder = new StakeholderReadModel(
            stakeholderId,
            user.Id,
            user.Email!,
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ConsumerTestData.FirstName(),
            ConsumerTestData.LastName(),
            null,
            false);
        identityService.FindByIdAsync(user.Id).Returns(user);
        stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        twoFactorOtpService.OtpExistsAsync(user.Id, OtpIntent.EmailConfirmation, Arg.Any<CancellationToken>())
            .Returns(true);
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var clock = TimeProvider.System;

        await new UserCreatedHandler(
            customTelemetryContext,
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            identityService,
            stakeholderRepository,
            new EmailConfirmationOtpSender(twoFactorOtpService, Substitute.For<ICommandSender>(), clock),
            Substitute.For<IUnitOfWork>(),
            clock,
            Substitute.For<ILogger<UserCreatedHandler>>(),
            Substitute.For<IRepository<MessageInbox>>()).HandleAsync(
            new UserCreatedEvent
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        customTelemetryContext.Received(1).AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSendSkipped,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.StakeholderId] == stakeholderId.ToString() &&
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.ActiveOtpExists));
    }
}
