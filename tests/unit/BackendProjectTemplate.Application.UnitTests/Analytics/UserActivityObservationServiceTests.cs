using BackendProjectTemplate.Application.Analytics;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Analytics;
using BackendProjectTemplate.Domain.Common.Messaging;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Analytics;

public sealed class UserActivityObservationServiceTests
{
    [Fact]
    public async Task When_ClaimingNewWindow_ShouldPersistOutboxEvent()
    {
        var context = new TestContext();
        var tenantId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var occurredAtUtc = new DateTimeOffset(2026, 11, 20, 0, 30, 0, TimeSpan.FromHours(1));
        context.Throttle.TryClaimAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        await context.Sut.ObserveAsync(
            new UserActivityObservation(
                tenantId,
                stakeholderId,
                occurredAtUtc,
                UserActivityType.AuthenticatedRequest),
            CancellationToken.None);

        await context.Publisher.Received(1).PublishAsync(
            Arg.Is<UserActivityObserved>(message =>
                message.TenantId == tenantId
                && message.StakeholderId == stakeholderId
                && message.OccuredAt == occurredAtUtc
                && message.ActivityDate == new DateOnly(2026, 11, 19)
                && message.ActivityType == UserActivityType.AuthenticatedRequest),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_WindowAlreadyClaimed_ShouldNotPublish()
    {
        var context = new TestContext();
        context.Throttle.TryClaimAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        await context.Sut.ObserveAsync(CreateObservation(), CancellationToken.None);

        await context.Publisher.DidNotReceiveWithAnyArgs().PublishAsync(default(UserActivityObserved)!, default);
        await context.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task When_RedisIsUnavailable_ShouldFailOpenToOutbox()
    {
        var context = new TestContext();
        context.Throttle.TryClaimAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException("Redis unavailable"));

        await context.Sut.ObserveAsync(CreateObservation(), CancellationToken.None);

        await context.Publisher.ReceivedWithAnyArgs(1).PublishAsync(default(UserActivityObserved)!, default);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_OutboxPersistenceFails_ShouldReleaseOwnedClaim()
    {
        var context = new TestContext();
        Guid claimedObservationId = default;
        string? claimedKey = null;
        context.Throttle.TryClaimAsync(
                Arg.Do<string>(value => claimedKey = value),
                Arg.Do<Guid>(value => claimedObservationId = value),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        context.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("Database unavailable"));

        var action = async () => await context.Sut.ObserveAsync(CreateObservation(), CancellationToken.None);

        await Should.ThrowAsync<InvalidOperationException>(action);
        await context.Throttle.Received(1).ReleaseAsync(
            claimedKey!,
            claimedObservationId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_ObservationsCrossWindows_ShouldUseDifferentClaimKeys()
    {
        var context = new TestContext();
        var keys = new List<string>();
        context.Throttle.TryClaimAsync(
                Arg.Do<string>(keys.Add),
                Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var first = CreateObservation() with
        {
            OccurredAtUtc = new DateTimeOffset(2026, 11, 20, 12, 4, 59, TimeSpan.Zero)
        };
        var second = first with { OccurredAtUtc = first.OccurredAtUtc.AddSeconds(1) };

        await context.Sut.ObserveAsync(first, CancellationToken.None);
        await context.Sut.ObserveAsync(second, CancellationToken.None);

        keys.Count.ShouldBe(2);
        keys[0].ShouldNotBe(keys[1]);
    }

    private static UserActivityObservation CreateObservation() =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero),
            UserActivityType.AuthenticatedRequest);

    private sealed class TestContext
    {
        public TestContext()
        {
            Sut = new UserActivityObservationService(
                Throttle,
                Publisher,
                UnitOfWork,
                Substitute.For<ILogger<UserActivityObservationService>>());
        }

        public IUserActivityThrottle Throttle { get; } = Substitute.For<IUserActivityThrottle>();
        public IEventPublisher Publisher { get; } = Substitute.For<IEventPublisher>();
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
        public UserActivityObservationService Sut { get; }
    }
}
