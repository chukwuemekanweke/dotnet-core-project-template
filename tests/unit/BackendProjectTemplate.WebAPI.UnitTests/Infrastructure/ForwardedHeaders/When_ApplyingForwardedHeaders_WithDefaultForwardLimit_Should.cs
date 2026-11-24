using Shouldly;
using System.Net;
using static BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders.ForwardedHeadersMiddlewareRunner;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders;

public sealed class When_ApplyingForwardedHeaders_WithDefaultForwardLimit_Should
{
    [Fact]
    public async Task UseOnlyTheLastHop()
    {
        var context = await InvokeAsync(
            new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = EdgeProxyGateway,
                ["ForwardedHeaders:KnownNetworks:0"] = PortalNetwork
            },
            remoteAddress: EdgeProxyGateway,
            forwardedFor: $"203.0.113.7, {PortalAddress}");

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse(PortalAddress));
    }
}
