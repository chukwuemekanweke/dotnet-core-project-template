using BackendProjectTemplate.Domain.Kyc.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Kyc.Webhooks;

public sealed class When_ReceivingKycaidWebhook_WithMalformedPayload_Should
{
    [Theory]
    [InlineData("{")]
    [InlineData("{\"request_id\":\"request-1\",\"type\":\"VERIFICATION_COMPLETED\"}")]
    public async Task RejectWithoutDatabaseMutation(string payload)
    {
        var context = new KycaidWebhooksControllerTestContext();

        var result = await context.HandleAsync(payload);

        result.ShouldBeOfType<BadRequestObjectResult>();
        await context.InboxRepository.DidNotReceive().AddAsync(
            Arg.Any<KycWebhookInbox>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
