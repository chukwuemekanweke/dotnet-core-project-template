using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUserSignInSuccessfulWithoutStakeholderActorId_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var loginActivityIpAddressResolver = Substitute.For<ILoginActivityIpAddressResolver>();
        var loginActivityRepository = Substitute.For<IRepository<LoginActivity>>();
        var userAgentParserService = Substitute.For<IUserAgentParserService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var tenantId = Guid.CreateVersion7();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

        var action = async () => await new UserSignInSuccessfulHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            identityService,
            stakeholderReadModelRepository,
            commandSender,
            loginActivityIpAddressResolver,
            loginActivityRepository,
            unitOfWork,
            userAgentParserService,
            TimeProvider.System).HandleAsync(
                new UserSignInSuccessful(ipAddress, userAgent)
                {
                    TenantId = tenantId
                },
                CancellationToken.None);

        await action.ShouldThrowAsync<CannotProcessMessageNonTransientException>();
        await identityService.DidNotReceive().FindByIdAsync(Arg.Any<Guid>());
    }
}

