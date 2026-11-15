using BackendProjectTemplate.Application.Kyc.Features.ReceiveVerificationCallback;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.ReceiveVerificationCallback;

public sealed class When_ReceivingKycCallback_WithConcurrentDuplicate_Should
{
    [Fact]
    public async Task AcknowledgeDuplicate()
    {
        var inboxRepository = Substitute.For<IRepository<KycWebhookInbox>>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        inboxRepository.AnyAsync(Arg.Any<ISpecification<KycWebhookInbox>>(), Arg.Any<CancellationToken>()).Returns(false);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new KycWebhookDuplicateException("duplicate", new InvalidOperationException()));
        var handler = new ReceiveKycCallbackHandler(inboxRepository, eventPublisher, unitOfWork, TimeProvider.System);

        var result = await handler.HandleAsync(
            new("request-1", "VERIFICATION_COMPLETED", "verification-1", "token-1", "applicant-1", "external-1", "completed", true),
            CancellationToken.None);

        result.ShouldBe(ReceiveKycCallbackStatus.Duplicate);
        unitOfWork.Received(1).DiscardChanges();
    }
}
