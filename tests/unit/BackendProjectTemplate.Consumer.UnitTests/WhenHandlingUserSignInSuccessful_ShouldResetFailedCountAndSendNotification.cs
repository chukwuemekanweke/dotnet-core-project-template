using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUserSignInSuccessful_ShouldResetFailedCountAndSendNotification
{
    [Fact]
    public async Task Verify()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var user = AppUser.Create(email, firstName, lastName, DateTimeOffset.UtcNow);

        identityService.FindByIdAsync(userId).Returns(user);
        identityService.ResetAccessFailedCountAsync(user).Returns(IdentityResult.Success);
        stakeholderReadModelRepository.GetByAppUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(Guid.CreateVersion7(), userId, tenantId, countryId, Guid.CreateVersion7()));

        await new UserSignInSuccessfulHandler(customTelemetryContext, identityService, stakeholderReadModelRepository, commandSender, unitOfWork).HandleAsync(
            new UserSignInSuccessful(userId, email, ipAddress, userAgent),
            CancellationToken.None);

        await identityService.Received(1).ResetAccessFailedCountAsync(user);
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.TenantId == tenantId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.SignInSuccessful &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["IpAddress"] == ipAddress &&
                ((EmailNotificationContent)command.NotificationContent).Content["UserAgent"] == userAgent),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
