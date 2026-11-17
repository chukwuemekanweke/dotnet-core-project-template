using BackendProjectTemplate.Domain.Kyc.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Kyc.Webhooks;

public sealed class When_ReceivingKycaidWebhook_WithInvalidSignature_Should
{
    [Theory]
    [InlineData(false, "invalid")]
    [InlineData(true, null)]
    public async Task RejectWithoutDatabaseMutation(bool validatorResult, string? signature)
    {
        var context = new KycaidWebhooksControllerTestContext(validatorResult);

        var result = await context.HandleAsync("{}", signature);

        result.ShouldBeOfType<UnauthorizedObjectResult>();
        await context.InboxRepository.DidNotReceive().AddAsync(
            Arg.Any<KycWebhookInbox>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
