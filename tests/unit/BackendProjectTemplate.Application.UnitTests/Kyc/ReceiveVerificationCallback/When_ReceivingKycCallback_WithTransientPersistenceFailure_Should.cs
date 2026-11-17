using BackendProjectTemplate.Application.Kyc.Features.ReceiveVerificationCallback;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.ReceiveVerificationCallback;

public sealed class When_ReceivingKycCallback_WithTransientPersistenceFailure_Should
{
    [Fact]
    public async Task RethrowForRetry()
    {
        var inboxRepository = Substitute.For<IRepository<KycWebhookInbox>>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var exception = new TimeoutException("database timeout");
        inboxRepository.AnyAsync(
                Arg.Any<ISpecification<KycWebhookInbox>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<int>(_ => throw exception);
        var handler = new ReceiveKycCallbackHandler(
            inboxRepository, eventPublisher, unitOfWork, TimeProvider.System);

        var thrown = await Should.ThrowAsync<TimeoutException>(() => handler.HandleAsync(
            new ReceiveKycCallbackCommand(
                "request-1", "VERIFICATION_COMPLETED", "verification-1", "token-1", "applicant-1",
                "external-1", "completed", true),
            CancellationToken.None));

        thrown.ShouldBeSameAs(exception);
        unitOfWork.DidNotReceive().DiscardChanges();
    }
}
