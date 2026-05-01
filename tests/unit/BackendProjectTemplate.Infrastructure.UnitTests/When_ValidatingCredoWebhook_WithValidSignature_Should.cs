using System.Security.Cryptography;
using System.Text;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.Infrastructure.Payments.Credo;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests;

public sealed class When_ValidatingCredoWebhook_WithValidSignature_Should
{
    [Fact]
    public async Task ReturnValidStatus()
    {
        var sut = new CredoWebhookSignatureValidator(
            Options.Create(new CredoOptions
            {
                SecretKey = "test_secret_key"
            }));
        var signature = ComputeSignature("test_secret_key", "700607002190001");

        var result = await sut.ValidateAsync(
            new CredoWebhookSignatureValidationRequest(
                signature,
                "700607002190001"),
            CancellationToken.None);

        result.SignatureValidationStatus.ShouldBe(SignatureValidationStatus.Valid);
        result.StatusChangeReason.ShouldBe("signature_verified");
    }

    private static string ComputeSignature(string secretKey, string businessCode)
    {
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes($"{secretKey}{businessCode}"));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
