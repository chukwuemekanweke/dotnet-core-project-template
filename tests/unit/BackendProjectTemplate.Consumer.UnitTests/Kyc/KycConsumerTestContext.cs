using BackendProjectTemplate.Consumer.Kyc;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer.UnitTests.Kyc;

internal sealed class KycConsumerTestContext
{
    public KycConsumerTestContext()
    {
        MessageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        ResultNormalizer.Normalize(Arg.Any<string?>(), Arg.Any<bool?>()).Returns(callInfo =>
        {
            var status = callInfo.ArgAt<string?>(0);
            var verified = callInfo.ArgAt<bool?>(1);
            if (status == "completed" && verified.HasValue)
            {
                return verified.Value ? KycVerificationTransition.Verified : KycVerificationTransition.Rejected;
            }

            return status == "pending" ? KycVerificationTransition.InProgress : KycVerificationTransition.NoChange;
        });
    }

    public IRepository<KycWebhookInbox> WebhookInboxRepository { get; } = Substitute.For<IRepository<KycWebhookInbox>>();
    public IRepository<KycVerification> VerificationRepository { get; } = Substitute.For<IRepository<KycVerification>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IKycVerificationResultNormalizer ResultNormalizer { get; } = Substitute.For<IKycVerificationResultNormalizer>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ICurrentActorAccessor CurrentActorAccessor { get; } = Substitute.For<ICurrentActorAccessor>();
    public IMessageContext MessageContext { get; } = Substitute.For<IMessageContext>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public TimeProvider Clock { get; } = new FixedTimeProvider(new DateTimeOffset(2026, 11, 15, 13, 0, 0, TimeSpan.Zero));
    public IRepository<MessageInbox> MessageInboxRepository { get; } = Substitute.For<IRepository<MessageInbox>>();
    public ILogger<KycWebhookReceivedHandler> Logger { get; } = Substitute.For<ILogger<KycWebhookReceivedHandler>>();

    public KycWebhookReceivedHandler CreateHandler() =>
        new(
            WebhookInboxRepository,
            VerificationRepository,
            StakeholderRepository,
            ResultNormalizer,
            EventPublisher,
            CurrentActorAccessor,
            MessageContext,
            CustomTelemetryContext,
            UnitOfWork,
            Clock,
            MessageInboxRepository,
            Logger);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
