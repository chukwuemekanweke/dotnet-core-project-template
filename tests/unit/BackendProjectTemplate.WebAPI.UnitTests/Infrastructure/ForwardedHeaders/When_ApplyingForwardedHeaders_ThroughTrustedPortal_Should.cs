using Shouldly;
using System.Net;
using static BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders.ForwardedHeadersMiddlewareRunner;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders;

public sealed class When_ApplyingForwardedHeaders_ThroughTrustedPortal_Should
{
    [Fact]
    public async Task UseBrowserAddressForwardedByPortal()
    {
        var browserAddress = "203.0.113.7";

        var context = await InvokeAsync(
            EdgeAndPortalTrust(forwardLimit: 2),
            remoteAddress: EdgeProxyGateway,
            forwardedFor: $"{browserAddress}, {PortalAddress}");

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse(browserAddress));
        context.Request.Scheme.ShouldBe("https");
    }
}
