using BackendProjectTemplate.Domain.Common.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace BackendProjectTemplate.Infrastructure.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionalOutbox(this IServiceCollection services)
    {
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<ICommandSender, CommandSender>();
        return services;
    }

    public static IServiceCollection AddRabbitMqOutboxDispatching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(RabbitMqMessagingOptions.SectionName)
            .Get<RabbitMqMessagingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{RabbitMqMessagingOptions.SectionName}' is required.");

        options.Validate();

        services.AddPublisher(
            new PublisherConfig
            {
                ServiceName = options.ServiceName,
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                EventsExchange = options.EventsExchange
            },
            RabbitMqOutboxMessageDispatcher.DependencyInjectionKey);

        services.AddSender(
            new SenderConfig
            {
                ServiceName = options.ServiceName,
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                CommandsExchange = options.CommandsExchange
            },
            RabbitMqOutboxMessageDispatcher.DependencyInjectionKey);

        services.AddScoped<IOutboxMessageDispatcher>(serviceProvider =>
            new RabbitMqOutboxMessageDispatcher(
                serviceProvider.GetRequiredKeyedService<IPublisher>(RabbitMqOutboxMessageDispatcher.DependencyInjectionKey),
                serviceProvider.GetRequiredKeyedService<ISender>(RabbitMqOutboxMessageDispatcher.DependencyInjectionKey)));

        return services;
    }
}
