using BackendProjectTemplate.Infrastructure.Kyc.Kycaid;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Kyc;

public sealed class WhenValidatingKycaidCallbackSignature_Should
{
    [Fact]
    public void AcceptExactRawPayloadAndRejectTampering()
    {
        const string apiKey = "test-api-key";
        var payload = Encoding.UTF8.GetBytes("{\"request_id\":\"request-1\",\"type\":\"VERIFICATION_COMPLETED\"}");
        var base64 = Convert.ToBase64String(payload);
        var signature = Convert.ToHexString(HMACSHA512.HashData(
            Encoding.UTF8.GetBytes(apiKey), Encoding.UTF8.GetBytes(base64))).ToLowerInvariant();
        var validator = new KycaidCallbackSignatureValidator(Options.Create(new KycaidOptions { ApiKey = apiKey }));

        validator.IsValid(payload, signature).ShouldBeTrue();
        validator.IsValid(Encoding.UTF8.GetBytes("{}"), signature).ShouldBeFalse();
        validator.IsValid(payload, null).ShouldBeFalse();
    }
}
