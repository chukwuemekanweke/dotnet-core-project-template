using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingInvalidCredentialsUserSignInFailed_ShouldIncrementFailedCountWithoutSendingAccountLockedNotification
{
    [Fact]
    public async Task Verify()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<UserSignInFailedHandler>>();
        var userId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var user = AppUser.Create(email, firstName, lastName, DateTimeOffset.UtcNow);

        identityService.FindByIdAsync(userId).Returns(user);
        identityService.AccessFailedAsync(user).Returns(IdentityResult.Success);
        identityService.IsLockedOutAsync(user).Returns(false);

        await new UserSignInFailedHandler(customTelemetryContext, identityService, stakeholderReadModelRepository, commandSender, unitOfWork, logger).HandleAsync(
            new UserSignInFailed(
                userId,
                email,
                ipAddress,
                userAgent,
                UserSignInFailureReasons.InvalidCredentials),
            CancellationToken.None);

        await identityService.Received(1).AccessFailedAsync(user);
        await commandSender.DidNotReceive().SendAsync(
            Arg.Any<SendNotificationCommand>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
