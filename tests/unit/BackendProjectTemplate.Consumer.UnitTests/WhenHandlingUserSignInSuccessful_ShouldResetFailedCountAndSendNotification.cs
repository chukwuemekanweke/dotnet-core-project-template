using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUserSignInSuccessful_ShouldResetFailedCountAndSendNotification
{
    [Fact]
    public async Task Verify()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var notificationSender = Substitute.For<IAuthenticationNotificationSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserSignInSuccessfulHandler>>();
        var userId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var user = AppUser.Create(email, firstName, lastName, DateTimeOffset.UtcNow);

        identityService.FindByIdAsync(userId).Returns(user);
        identityService.ResetAccessFailedCountAsync(user).Returns(IdentityResult.Success);

        await new UserSignInSuccessfulHandler(customTelemetryContext, identityService, notificationSender, logger).HandleAsync(
            new UserSignInSuccessful(userId, email, "127.0.0.1", "UnitTestAgent/1.0"),
            CancellationToken.None);

        await identityService.Received(1).ResetAccessFailedCountAsync(user);
        await notificationSender.Received(1).SendSignInSuccessfulAsync(
            user,
            "127.0.0.1",
            "UnitTestAgent/1.0",
            Arg.Any<CancellationToken>());
    }
}
