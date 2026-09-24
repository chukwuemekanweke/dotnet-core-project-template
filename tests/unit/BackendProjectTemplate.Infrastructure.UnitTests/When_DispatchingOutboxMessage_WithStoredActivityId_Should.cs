using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using NSubstitute;
using Shouldly;
using System.Diagnostics;
using System.Text.Json;
using DomainObservability = BackendProjectTemplate.Domain.Common.Observability.Observability;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class When_DispatchingOutboxMessage_WithStoredActivityId_Should
{
    [Fact]
    public async Task PreserveTheOriginatingTraceId()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var parentActivityId = $"00-{traceId}-{spanId}-01";
        var @event = new UserCreated
        {
            StakeholderId = Guid.CreateVersion7()
        };

        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "BackendProjectTemplate",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.GetTagItem(DomainObservability.PropertyNames.Common.MessageId)?.ToString() == @event.MessageId.ToString())
                {
                    capturedActivity = activity;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var publisher = Substitute.For<IPublisher>();
        var sender = Substitute.For<ISender>();
        var outboxMessage = OutboxMessage.CreateEvent(
            @event.MessageId,
            typeof(UserCreated).FullName.ShouldNotBeNull(),
            JsonSerializer.Serialize(@event),
            @event.OccuredAt,
            @event.OccuredAt,
            activityId: parentActivityId);

        var sut = new RabbitMqOutboxMessageDispatcher(publisher, sender);

        await sut.DispatchAsync(outboxMessage, CancellationToken.None);

        capturedActivity.ShouldNotBeNull();
        capturedActivity.TraceId.ShouldBe(traceId);
        capturedActivity.ParentSpanId.ShouldBe(spanId);
    }
}
