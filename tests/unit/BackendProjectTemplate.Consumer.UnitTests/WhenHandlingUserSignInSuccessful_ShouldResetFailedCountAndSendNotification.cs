using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUserSignInSuccessful_ShouldResetFailedCountAndSendNotification
{
    [Fact]
    public async Task Verify()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var user = AppUser.Create(email, firstName, lastName, DateTimeOffset.UtcNow);
        var appUserId = Guid.CreateVersion7();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        identityService.FindByIdAsync(appUserId).Returns(user);
        identityService.ResetAccessFailedCountAsync(user).Returns(IdentityResult.Success);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, tenantId, countryId, Guid.CreateVersion7(), "Ada", "Lovelace", null, false));

        await new UserSignInSuccessfulHandler(customTelemetryContext, currentActorAccessor, messageContext, identityService, stakeholderReadModelRepository, commandSender, unitOfWork).HandleAsync(
            new UserSignInSuccessful(ipAddress, userAgent)
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await identityService.Received(1).ResetAccessFailedCountAsync(user);
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.TenantId == tenantId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.SignInSuccessful &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.StakeholderId == stakeholderId &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["IpAddress"] == ipAddress &&
                ((EmailNotificationContent)command.NotificationContent).Content["UserAgent"] == userAgent),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
