using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Credo;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class When_ValidatingCredoWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnInvalidStatus()
    {
        var sut = new CredoWebhookSignatureValidator(
            Options.Create(new CredoOptions
            {
                SecretKey = "test_secret_key"
            }));

        var result = await sut.ValidateAsync(
            new CredoWebhookSignatureValidationRequest(
                "invalid-signature",
                "700607002190001"),
            CancellationToken.None);

        result.SignatureValidationStatus.ShouldBe(SignatureValidationStatus.Invalid);
        result.StatusChangeReason.ShouldBe(KnownWebhookStatusChangeReasons.Shared.InvalidSignature);
    }
}
