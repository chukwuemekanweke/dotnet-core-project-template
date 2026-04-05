using BackendProjectTemplate.Domain.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionalOutbox(this IServiceCollection services)
    {
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IOutboxMessageDispatcher, LoggingOutboxMessageDispatcher>();
        return services;
    }
}
