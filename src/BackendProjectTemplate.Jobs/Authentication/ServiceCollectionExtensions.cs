using BackendProjectTemplate.Infrastructure.Authentication;
using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;

namespace BackendProjectTemplate.Jobs.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIpAddressLocationEnrichment(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IpAddressLocationEnrichmentOptions>(
            configuration.GetSection(IpAddressLocationEnrichmentOptions.SectionName));
        services.AddIpGeolocationServices(configuration);
        services.AddSingleton(new BackgroundServiceDescriptor(IpAddressLocationEnrichmentProcessor.ServiceName));
        services.AddHostedService<IpAddressLocationEnrichmentProcessor>();

        return services;
    }
}
