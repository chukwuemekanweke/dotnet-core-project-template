using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUserSignInSuccessfulWithoutUserId_ShouldThrowNonTransientException
{
    [Fact]
    public async Task Verify()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var notificationSender = Substitute.For<IAuthenticationNotificationSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserSignInSuccessfulHandler>>();
        var email = ConsumerTestData.Email();

        var action = async () => await new UserSignInSuccessfulHandler(
            customTelemetryContext,
            identityService,
            notificationSender,
            logger).HandleAsync(
                new UserSignInSuccessful(Guid.Empty, email, "127.0.0.1", "UnitTestAgent/1.0"),
                CancellationToken.None);

        await action.ShouldThrowAsync<CannotProcessMessageNonTransientException>();
        await identityService.DidNotReceive().FindByIdAsync(Arg.Any<Guid>());
    }
}
