using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.WebAPI.Features.Payments.Webhooks.Bachs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Payments.Webhooks.Bachs;

public sealed class When_ReceivingBachsWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnBadRequestAfterValidatingOriginalBody()
    {
        const string payload = "{ \"id\": \"evt_123\", \"type\": \"collection.succeeded\", \"created_at\": \"2026-11-14T12:00:00Z\", \"organization_id\": \"acct_123\", \"data\": { \"checkout_id\": \"chk_123\" } }";
        var context = new PaymentsControllerTestContext();
        var provider = context.CreatePaymentProvider("Bachs", PaymentProviderKeys.Bachs);
        context.PaymentProviderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentProvider>>(),
                Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentWebhookInbox>>(),
                Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.BachsWebhookSignatureValidator.ValidateAsync(
                Arg.Any<BachsWebhookSignatureValidationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(
                SignatureValidationStatus.Invalid,
                "invalid_signature"));
        var sut = new BachsWebhooksController(context.CreateBachsWebhookHandler());
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        sut.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        sut.Request.Headers["X-Bachs-Signature-V2"] = "t=1,v1=invalid";

        var result = await sut.Handle(CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
        await context.BachsWebhookSignatureValidator.Received(1).ValidateAsync(
            Arg.Is<BachsWebhookSignatureValidationRequest>(request =>
                Encoding.UTF8.GetString(request.RawBody) == payload &&
                request.SignatureV2Header == "t=1,v1=invalid"),
            Arg.Any<CancellationToken>());
    }
}
