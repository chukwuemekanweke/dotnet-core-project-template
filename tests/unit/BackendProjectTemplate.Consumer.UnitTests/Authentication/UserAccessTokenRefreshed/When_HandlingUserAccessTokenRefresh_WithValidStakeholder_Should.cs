using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer.UnitTests.Authentication;

public sealed class When_HandlingUserAccessTokenRefresh_WithValidStakeholder_Should
{
    [Fact]
    public async Task PersistSecurityActivity()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var securityActivityIpAddressResolver = Substitute.For<ISecurityActivityIpAddressResolver>();
        var securityActivityRepository = Substitute.For<IRepository<SecurityActivity>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var userAgentParserService = Substitute.For<IUserAgentParserService>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero));
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var message = new UserAccessTokenRefreshed(ConsumerTestData.IpAddress(), ConsumerTestData.UserAgent())
        {
            StakeholderId = stakeholderId,
            TenantId = tenantId
        };

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                stakeholderId,
                Guid.CreateVersion7(),
                ConsumerTestData.Email(),
                tenantId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                ConsumerTestData.FirstName(),
                ConsumerTestData.LastName(),
                null,
                true));
        securityActivityIpAddressResolver.ResolveAsync(message.IpAddress, Arg.Any<CancellationToken>())
            .Returns(new SecurityActivityIpAddressResolution(Guid.CreateVersion7(), Guid.CreateVersion7()));
        userAgentParserService.Parse(message.UserAgent)
            .Returns(new UserAgentInfo("Phone", "Android", "Chrome"));

        var sut = new UserAccessTokenRefreshedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            stakeholderReadModelRepository,
            securityActivityIpAddressResolver,
            securityActivityRepository,
            unitOfWork,
            userAgentParserService,
            timeProvider,
            messageInboxRepository,
            Substitute.For<ILogger<UserAccessTokenRefreshedHandler>>());

        await sut.HandleAsync(message, CancellationToken.None);

        await securityActivityRepository.Received(1).AddAsync(
            Arg.Is<SecurityActivity>(activity =>
                activity.StakeholderId == stakeholderId &&
                activity.TenantId == tenantId &&
                activity.ActivityType == SecurityActivityType.TokenRefresh),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
