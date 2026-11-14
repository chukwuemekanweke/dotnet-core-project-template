using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_ValidatingBachsWebhook_WithMissingSignature_Should
{
    [Fact]
    public async Task RejectWebhook()
    {
        var sut = new BachsWebhookSignatureValidator(
            Options.Create(new BachsOptions { WebhookSecret = "whsec_test" }),
            TimeProvider.System);

        var result = await sut.ValidateAsync(
            new BachsWebhookSignatureValidationRequest(System.Text.Encoding.UTF8.GetBytes("{}"), null, null, null),
            CancellationToken.None);

        result.SignatureValidationStatus.ShouldBe(SignatureValidationStatus.Invalid);
    }
}
