using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_ValidatingBachsWebhook_WithRotatingSignature_Should
{
    [Fact]
    public async Task AcceptAnyMatchingV1Signature()
    {
        const string secret = "whsec_test";
        const string payload = "{\"id\":\"evt_123\"}";
        const long timestamp = 1794657600;
        var expected = Convert.ToHexStringLower(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes($"{timestamp}.{payload}")));
        var sut = new BachsWebhookSignatureValidator(
            Options.Create(new BachsOptions { WebhookSecret = secret }),
            new TestTimeProvider(DateTimeOffset.FromUnixTimeSeconds(timestamp)));

        var result = await sut.ValidateAsync(
            new BachsWebhookSignatureValidationRequest(
                Encoding.UTF8.GetBytes(payload),
                $"t={timestamp},v1={new string('0', 64)},v1={expected}",
                null,
                null),
            CancellationToken.None);

        result.SignatureValidationStatus.ShouldBe(SignatureValidationStatus.Valid);
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
