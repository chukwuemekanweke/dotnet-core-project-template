using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Kyc.Webhooks;

public sealed class When_ReceivingKycaidWebhook_WithValidPayload_Should
{
    [Fact]
    public async Task PersistBeforeAcknowledging()
    {
        var context = new KycaidWebhooksControllerTestContext();

        var result = await context.HandleAsync(
            """{"request_id":"request-1","type":"VERIFICATION_COMPLETED","verification_id":"verification-1","applicant_id":"applicant-1","status":"completed","verified":true}""");

        result.ShouldBeOfType<OkResult>();
        await context.InboxRepository.Received(1).AddAsync(
            Arg.Any<KycWebhookInbox>(), Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Any<KycWebhookReceived>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
