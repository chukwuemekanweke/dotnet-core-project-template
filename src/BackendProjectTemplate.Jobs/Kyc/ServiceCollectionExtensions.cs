using BackendProjectTemplate.Infrastructure.Kyc;
using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;

namespace BackendProjectTemplate.Jobs.Kyc;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKycReconciliation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKycServices(configuration);
        services.Configure<KycReconciliationOptions>(configuration.GetSection(KycReconciliationOptions.SectionName));
        services.AddSingleton(new BackgroundServiceDescriptor(KycReconciliationProcessor.ServiceName));
        services.AddHostedService<KycReconciliationProcessor>();

        return services;
    }
}
