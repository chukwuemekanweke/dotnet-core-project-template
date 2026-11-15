using BackendProjectTemplate.Application.Kyc.Features.ReceiveVerificationCallback;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc;

public sealed class WhenReceivingKycCallbacks_Should
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PersistCallbackAndPublishEventAtomically()
    {
        var inboxRepository = Substitute.For<IRepository<KycWebhookInbox>>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        KycWebhookInbox? persistedInbox = null;
        KycWebhookReceived? publishedEvent = null;
        inboxRepository.AnyAsync(Arg.Any<ISpecification<KycWebhookInbox>>(), Arg.Any<CancellationToken>()).Returns(false);
        inboxRepository.AddAsync(Arg.Do<KycWebhookInbox>(value => persistedInbox = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        eventPublisher.PublishAsync(Arg.Do<KycWebhookReceived>(value => publishedEvent = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = new ReceiveKycCallbackHandler(inboxRepository, eventPublisher, unitOfWork, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new("request-1", "VERIFICATION_COMPLETED", "verification-1", "token-1", "applicant-1",
                "external-1", "completed", true), CancellationToken.None);

        result.ShouldBe(ReceiveKycCallbackStatus.Persisted);
        persistedInbox.ShouldNotBeNull();
        persistedInbox.RequestId.ShouldBe("request-1");
        persistedInbox.ProcessedAtUtc.ShouldBeNull();
        publishedEvent.ShouldNotBeNull();
        publishedEvent.WebhookInboxId.ShouldBe(persistedInbox.Id);
        publishedEvent.RequestId.ShouldBe(persistedInbox.RequestId);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AcknowledgeDuplicateWithoutPublishingAgain()
    {
        var inboxRepository = Substitute.For<IRepository<KycWebhookInbox>>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        inboxRepository.AnyAsync(Arg.Any<ISpecification<KycWebhookInbox>>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new ReceiveKycCallbackHandler(inboxRepository, eventPublisher, unitOfWork, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new("request-1", "VERIFICATION_COMPLETED", null, null, null, null, "pending", null), CancellationToken.None);

        result.ShouldBe(ReceiveKycCallbackStatus.Duplicate);
        await inboxRepository.DidNotReceive().AddAsync(Arg.Any<KycWebhookInbox>(), Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishAsync(Arg.Any<KycWebhookReceived>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
