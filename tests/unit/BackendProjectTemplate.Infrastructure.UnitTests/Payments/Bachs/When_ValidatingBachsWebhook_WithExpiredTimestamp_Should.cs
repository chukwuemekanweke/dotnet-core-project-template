using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_ValidatingBachsWebhook_WithExpiredTimestamp_Should
{
    [Fact]
    public async Task RejectSignature()
    {
        const long timestamp = 1794657600;
        var sut = new BachsWebhookSignatureValidator(
            Options.Create(new BachsOptions { WebhookSecret = "whsec_test" }),
            new TestTimeProvider(DateTimeOffset.FromUnixTimeSeconds(timestamp + 301)));

        var result = await sut.ValidateAsync(
            new BachsWebhookSignatureValidationRequest(
                System.Text.Encoding.UTF8.GetBytes("{}"),
                $"t={timestamp},v1={new string('0', 64)}",
                null,
                null),
            CancellationToken.None);

        result.SignatureValidationStatus.ShouldBe(SignatureValidationStatus.Invalid);
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
