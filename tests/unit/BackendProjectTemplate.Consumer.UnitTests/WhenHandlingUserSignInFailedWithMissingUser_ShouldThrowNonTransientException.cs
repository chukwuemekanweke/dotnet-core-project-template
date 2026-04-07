using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUserSignInFailedWithMissingUser_ShouldThrowNonTransientException
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

        identityService.FindByIdAsync(userId).Returns((BackendProjectTemplate.Domain.Authentication.Entities.AppUser?)null);

        var action = async () => await new UserSignInFailedHandler(
            customTelemetryContext,
            identityService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork,
            logger).HandleAsync(
                new UserSignInFailed(
                    userId,
                    email,
                    ipAddress,
                    userAgent,
                    UserSignInFailureReasons.InvalidCredentials),
                CancellationToken.None);

        await action.ShouldThrowAsync<CannotProcessMessageNonTransientException>();
    }
}
