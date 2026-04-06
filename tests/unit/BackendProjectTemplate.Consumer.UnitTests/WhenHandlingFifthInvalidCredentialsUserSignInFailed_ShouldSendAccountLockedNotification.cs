using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
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
        var notificationSender = Substitute.For<IAuthenticationNotificationSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserSignInFailedHandler>>();
        var userId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var lockedUntilUtc = new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email, firstName, lastName, DateTimeOffset.UtcNow);

        identityService.FindByIdAsync(userId).Returns(user);
        identityService.AccessFailedAsync(user).Returns(IdentityResult.Success);
        identityService.IsLockedOutAsync(user).Returns(true);
        identityService.GetLockoutEndUtcAsync(user).Returns(lockedUntilUtc);

        await new UserSignInFailedHandler(customTelemetryContext, identityService, notificationSender, logger).HandleAsync(
            new UserSignInFailed(
                userId,
                email,
                "127.0.0.1",
                "UnitTestAgent/1.0",
                UserSignInFailureReasons.InvalidCredentials),
            CancellationToken.None);

        await notificationSender.Received(1).SendAccountLockedAsync(
            user,
            lockedUntilUtc,
            Arg.Any<CancellationToken>());
    }
}
