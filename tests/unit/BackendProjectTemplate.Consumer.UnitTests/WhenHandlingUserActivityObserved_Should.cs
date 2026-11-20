using BackendProjectTemplate.Consumer.Analytics;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Analytics;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenHandlingUserActivityObserved_Should
{
    [Fact]
    public async Task PersistStableObservationIdentity()
    {
        var activityPersistence = Substitute.For<IUserDailyActivityPersistence>();
        var messageId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var activityDate = new DateOnly(2026, 11, 20);
        var occurredAtUtc = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var messageContext = Substitute.For<IMessageContext>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

        var sut = new UserActivityObservedHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            activityPersistence,
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System,
            Substitute.For<IRepository<MessageInbox>>(),
            Substitute.For<ILogger<UserActivityObservedHandler>>());

        await sut.HandleAsync(new UserActivityObserved(activityDate, UserActivityType.AuthenticatedRequest)
        {
            MessageId = messageId,
            TenantId = tenantId,
            StakeholderId = stakeholderId,
            OccuredAt = occurredAtUtc
        }, CancellationToken.None);

        await activityPersistence.Received(1).ObserveAsync(
            messageId,
            tenantId,
            stakeholderId,
            activityDate,
            occurredAtUtc,
            UserActivityType.AuthenticatedRequest,
            Arg.Any<CancellationToken>());
    }
}
