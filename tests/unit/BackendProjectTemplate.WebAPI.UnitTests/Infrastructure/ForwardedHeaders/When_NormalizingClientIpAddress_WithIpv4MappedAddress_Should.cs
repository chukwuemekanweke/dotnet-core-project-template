using BackendProjectTemplate.WebAPI.Infrastructure.ForwardedHeaders;
using Microsoft.AspNetCore.Http;
using Shouldly;
using System.Net;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders;

public sealed class When_NormalizingClientIpAddress_WithIpv4MappedAddress_Should
{
    [Theory]
    [InlineData("::ffff:172.18.0.1", "172.18.0.1")]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("2001:db8::7", "2001:db8::7")]
    public async Task UsePlainIpv4ForMappedAddressesOnly(string remoteAddress, string expectedAddress)
    {
        var nextCalled = false;
        var middleware = new ClientIpAddressNormalizationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);

        await middleware.InvokeAsync(context);

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse(expectedAddress));
        nextCalled.ShouldBeTrue();
    }
}
