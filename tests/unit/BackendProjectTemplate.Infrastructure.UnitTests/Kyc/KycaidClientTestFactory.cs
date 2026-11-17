using BackendProjectTemplate.Infrastructure.Kyc.Kycaid;
using Microsoft.Extensions.Options;
using System.Net;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Kyc;

internal static class KycaidClientTestFactory
{
    public static KycaidClient Create(HttpStatusCode statusCode, bool enabled = true) =>
        new(
            new HttpClient(new StaticResponseHandler(statusCode)) { BaseAddress = new Uri("https://api.kycaid.test") },
            Options.Create(new KycaidOptions { Enabled = enabled, ApiKey = "key", FormId = "form-1" }),
            new KycaidVerificationResultNormalizer());

    private sealed class StaticResponseHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
