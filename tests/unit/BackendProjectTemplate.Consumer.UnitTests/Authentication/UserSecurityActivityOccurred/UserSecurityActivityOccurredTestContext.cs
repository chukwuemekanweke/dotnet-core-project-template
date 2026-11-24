using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using SecurityActivityOccurredEvent = BackendProjectTemplate.Contracts.Events.UserSecurityActivityOccurred;

namespace BackendProjectTemplate.Consumer.UnitTests.Authentication.UserSecurityActivityOccurred;

internal sealed class UserSecurityActivityOccurredTestContext
{
    public UserSecurityActivityOccurredTestContext()
    {
        MessageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
    }

    public IMessageContext MessageContext { get; } = Substitute.For<IMessageContext>();
    public IStakeholderReadModelRepository StakeholderReadModelRepository { get; } = Substitute.For<IStakeholderReadModelRepository>();
    public ISecurityActivityIpAddressResolver IpAddressResolver { get; } = Substitute.For<ISecurityActivityIpAddressResolver>();
    public IRepository<SecurityActivity> SecurityActivityRepository { get; } = Substitute.For<IRepository<SecurityActivity>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IUserAgentParserService UserAgentParserService { get; } = Substitute.For<IUserAgentParserService>();

    public static SecurityActivityOccurredEvent CreateMessage(string activityType, Guid? stakeholderId) =>
        new(activityType, ConsumerTestData.IpAddress(), ConsumerTestData.UserAgent())
        {
            StakeholderId = stakeholderId,
            TenantId = Guid.CreateVersion7(),
            OccuredAt = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero)
        };

    public StakeholderReadModel ArrangeStakeholder(Guid stakeholderId)
    {
        var stakeholder = new StakeholderReadModel(
            stakeholderId,
            Guid.CreateVersion7(),
            ConsumerTestData.Email(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ConsumerTestData.FirstName(),
            ConsumerTestData.LastName(),
            null,
            true);
        StakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        return stakeholder;
    }

    public UserSecurityActivityOccurredHandler CreateHandler() => new(
        Substitute.For<ICustomTelemetryContext>(),
        Substitute.For<ICurrentActorAccessor>(),
        MessageContext,
        StakeholderReadModelRepository,
        IpAddressResolver,
        SecurityActivityRepository,
        UnitOfWork,
        UserAgentParserService,
        TimeProvider.System,
        Substitute.For<IRepository<MessageInbox>>(),
        Substitute.For<ILogger<UserSecurityActivityOccurredHandler>>());
}
