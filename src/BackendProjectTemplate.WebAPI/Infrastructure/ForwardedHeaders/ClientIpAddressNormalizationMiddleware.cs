namespace BackendProjectTemplate.WebAPI.Infrastructure.ForwardedHeaders;

/// <summary>
/// Unwraps IPv4-mapped IPv6 client addresses (for example "::ffff:203.0.113.7") into plain IPv4 so sessions,
/// security activity and rate-limit partitions record one canonical form per client. Runs after the forwarded
/// headers middleware, so it sees the resolved client address.
/// </summary>
public sealed class ClientIpAddressNormalizationMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        if (remoteIpAddress is { IsIPv4MappedToIPv6: true })
        {
            context.Connection.RemoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        return next(context);
    }
}
