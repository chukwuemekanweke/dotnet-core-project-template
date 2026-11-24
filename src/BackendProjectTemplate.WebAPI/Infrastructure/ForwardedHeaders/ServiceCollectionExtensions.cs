using System.Net;
using HttpForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders;
using SystemIPNetwork = System.Net.IPNetwork;

namespace BackendProjectTemplate.WebAPI.Infrastructure.ForwardedHeaders;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var trust = configuration.GetSection(ForwardedHeadersTrustOptions.SectionName).Get<ForwardedHeadersTrustOptions>()
            ?? new ForwardedHeadersTrustOptions();

        var proxies = trust.KnownProxies
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => IPAddress.TryParse(value.Trim(), out var address)
                ? address
                : throw new InvalidOperationException(
                    $"Configuration value '{ForwardedHeadersTrustOptions.SectionName}:{nameof(ForwardedHeadersTrustOptions.KnownProxies)}' contains an invalid IP address '{value}'."))
            .ToArray();

        var networks = trust.KnownNetworks
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => SystemIPNetwork.TryParse(value.Trim(), out var network)
                ? network
                : throw new InvalidOperationException(
                    $"Configuration value '{ForwardedHeadersTrustOptions.SectionName}:{nameof(ForwardedHeadersTrustOptions.KnownNetworks)}' contains an invalid CIDR network '{value}'."))
            .ToArray();

        if (trust.ForwardLimit is < 1)
        {
            throw new InvalidOperationException(
                $"Configuration value '{ForwardedHeadersTrustOptions.SectionName}:{nameof(ForwardedHeadersTrustOptions.ForwardLimit)}' must be at least 1 but was '{trust.ForwardLimit}'.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = HttpForwardedHeaders.XForwardedFor | HttpForwardedHeaders.XForwardedProto;

            if (trust.ForwardLimit.HasValue)
            {
                options.ForwardLimit = trust.ForwardLimit.Value;
            }

            foreach (var proxy in proxies)
            {
                options.KnownProxies.Add(proxy);
            }

            foreach (var network in networks)
            {
                options.KnownIPNetworks.Add(network);
            }
        });

        return services;
    }
}
