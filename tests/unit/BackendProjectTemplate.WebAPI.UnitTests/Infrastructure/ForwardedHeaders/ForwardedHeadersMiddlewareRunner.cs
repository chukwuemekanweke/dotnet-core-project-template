using BackendProjectTemplate.WebAPI.Infrastructure.ForwardedHeaders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace BackendProjectTemplate.WebAPI.UnitTests.Infrastructure.ForwardedHeaders;

internal static class ForwardedHeadersMiddlewareRunner
{
    public const string EdgeProxyGateway = "172.30.231.1";
    public const string PortalNetwork = "172.21.0.0/16";
    public const string PortalAddress = "172.21.0.4";

    public static Dictionary<string, string?> EdgeAndPortalTrust(int forwardLimit) => new()
    {
        ["ForwardedHeaders:KnownProxies:0"] = EdgeProxyGateway,
        ["ForwardedHeaders:KnownNetworks:0"] = PortalNetwork,
        ["ForwardedHeaders:ForwardLimit"] = forwardLimit.ToString()
    };

    public static async Task<HttpContext> InvokeAsync(
        Dictionary<string, string?> settings,
        string remoteAddress,
        string forwardedFor,
        string forwardedProto = "https")
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
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;

        await middleware.Invoke(context);

        return context;
    }
}
