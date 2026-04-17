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
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingFifthInvalidCredentialsUserSignInFailed_ShouldSendAccountLockedNotification
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
        var logger = Substitute.For<ILogger<UserSignInFailedHandler>>();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var lockedUntilUtc = new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email, firstName, lastName, DateTimeOffset.UtcNow);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        identityService.FindByEmailAsync(email).Returns(user);
        identityService.AccessFailedAsync(user).Returns(IdentityResult.Success);
        identityService.IsLockedOutAsync(user).Returns(true);
        identityService.GetLockoutEndUtcAsync(user).Returns(lockedUntilUtc);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, Guid.CreateVersion7(), email, tenantId, countryId, Guid.CreateVersion7(), "Ada", "Lovelace", null, false));

        await new UserSignInFailedHandler(customTelemetryContext, currentActorAccessor, messageContext, identityService, stakeholderReadModelRepository, commandSender, unitOfWork, logger).HandleAsync(
            new UserSignInFailed(
                email,
                ipAddress,
                userAgent,
                UserSignInFailureReasons.InvalidCredentials)
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.TenantId == tenantId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.AccountLocked &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.StakeholderId == stakeholderId &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["LockedUntilUtc"] == lockedUntilUtc.ToString("O")),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
