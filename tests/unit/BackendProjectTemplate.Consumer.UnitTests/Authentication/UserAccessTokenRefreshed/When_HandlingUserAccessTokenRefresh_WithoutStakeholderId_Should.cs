using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Authentication;

public sealed class When_HandlingUserAccessTokenRefresh_WithoutStakeholderId_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var messageContext = Substitute.For<IMessageContext>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

        var sut = new UserAccessTokenRefreshedHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            Substitute.For<IStakeholderReadModelRepository>(),
            Substitute.For<ILoginActivityIpAddressResolver>(),
            Substitute.For<IRepository<LoginActivity>>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IUserAgentParserService>(),
            TimeProvider.System);

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            sut.HandleAsync(
                new UserAccessTokenRefreshed(ConsumerTestData.IpAddress(), ConsumerTestData.UserAgent())
                {
                    TenantId = Guid.CreateVersion7()
                },
                CancellationToken.None));

        exception.Message.ShouldContain("stakeholder actor id", Case.Insensitive);
    }
}
