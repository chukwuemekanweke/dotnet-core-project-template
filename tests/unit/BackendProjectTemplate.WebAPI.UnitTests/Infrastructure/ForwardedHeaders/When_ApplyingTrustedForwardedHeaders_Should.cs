using BackendProjectTemplate.WebAPI.Infrastructure.ForwardedHeaders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Net;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders;

public sealed class When_ApplyingTrustedForwardedHeaders_Should
{
    private const string ProxyGateway = "172.30.231.1";
    private const string ClientAddress = "203.0.113.7";

    [Fact]
    public async Task UseForwardedClientAndSchemeFromConfiguredProxy()
    {
        var context = await InvokeAsync(
            new Dictionary<string, string?> { ["ForwardedHeaders:KnownProxies:0"] = ProxyGateway },
            remoteAddress: ProxyGateway);

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse(ClientAddress));
        context.Request.Scheme.ShouldBe("https");
    }

    [Fact]
    public async Task UseForwardedClientFromConfiguredNetwork()
    {
        var context = await InvokeAsync(
            new Dictionary<string, string?> { ["ForwardedHeaders:KnownNetworks:0"] = "172.30.231.0/24" },
            remoteAddress: "172.30.231.9");

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse(ClientAddress));
    }

    [Fact]
    public async Task IgnoreForwardedHeadersFromUntrustedSource()
    {
        var context = await InvokeAsync(new Dictionary<string, string?>(), remoteAddress: ProxyGateway);

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse(ProxyGateway));
        context.Request.Scheme.ShouldBe("http");
    }

    [Theory]
    [InlineData("ForwardedHeaders:KnownProxies:0", "not-an-ip")]
    [InlineData("ForwardedHeaders:KnownNetworks:0", "172.30.231.0/99")]
    public void RejectInvalidTrustConfiguration(string key, string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(() => new ServiceCollection().AddTrustedForwardedHeaders(configuration));

        exception.Message.ShouldContain(value);
    }

    private static async Task<HttpContext> InvokeAsync(Dictionary<string, string?> settings, string remoteAddress)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        using var provider = new ServiceCollection().AddTrustedForwardedHeaders(configuration).BuildServiceProvider();
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>());

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = ClientAddress;
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(context);

        return context;
    }
}
