using Shouldly;
using System.Net;
using static BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders.ForwardedHeadersMiddlewareRunner;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders;

public sealed class When_ApplyingForwardedHeaders_WithSpoofedEntryFromBrowser_Should
{
    [Fact]
    public async Task StopAtFirstUntrustedAddress()
    {
        var browserAddress = "203.0.113.7";

        var context = await InvokeAsync(
            EdgeAndPortalTrust(forwardLimit: 2),
            remoteAddress: EdgeProxyGateway,
            forwardedFor: $"198.51.100.99, {browserAddress}");

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse(browserAddress));
    }
}
