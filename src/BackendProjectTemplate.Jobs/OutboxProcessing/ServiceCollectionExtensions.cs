using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;

namespace BackendProjectTemplate.Jobs.OutboxProcessing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxMessageProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OutboxProcessingOptions>(configuration.GetSection(OutboxProcessingOptions.SectionName));
        services.AddSingleton<OutboxProcessingSignal>();
        services.AddSingleton(new BackgroundServiceDescriptor(OutboxMessageProcessor.ServiceName));
        services.AddHostedService<OutboxMessageProcessor>();
        services.AddHostedService<OutboxNotificationListener>();

        return services;
    }
}
