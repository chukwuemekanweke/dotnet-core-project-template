using BackendProjectTemplate.Application.Kyc.Features.ReceiveVerificationCallback;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.WebAPI.Features.Kyc.Webhooks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Kyc.Webhooks;

internal sealed class KycaidWebhooksControllerTestContext(bool signatureIsValid = true)
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    public IRepository<KycWebhookInbox> InboxRepository { get; } = Substitute.For<IRepository<KycWebhookInbox>>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public ICustomTelemetryContext TelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();

    public async Task<IActionResult> HandleAsync(string payload, string? signature = "signature")
    {
        var handler = new ReceiveKycCallbackHandler(
            InboxRepository,
            EventPublisher,
            UnitOfWork,
            new FixedTimeProvider(Now));
        var controller = new KycaidWebhooksController(
            new TestSignatureValidator(signatureIsValid),
            handler,
            TelemetryContext)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        if (signature is not null)
        {
            controller.Request.Headers["x-data-integrity"] = signature;
        }

        return await controller.Handle(CancellationToken.None);
    }

    private sealed class TestSignatureValidator(bool isValid) : IKycCallbackSignatureValidator
    {
        public bool IsValid(ReadOnlySpan<byte> rawPayload, string? signature) => isValid && !string.IsNullOrWhiteSpace(signature);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
