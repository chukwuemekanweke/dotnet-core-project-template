using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer.DependencyInjection;
using ResetPassword = BackendProjectTemplate.Contracts.Commands.Authentication.ResetPasswordCommand;
using SendNotification = BackendProjectTemplate.Contracts.Commands.Notifications.SendNotificationCommand;
using UserAccessTokenRefreshedEvent = BackendProjectTemplate.Contracts.Events.UserAccessTokenRefreshed;
using UserCreatedEvent = BackendProjectTemplate.Contracts.Events.UserCreated;
using UserSignInFailedEvent = BackendProjectTemplate.Contracts.Events.UserSignInFailed;
using UserSignInSuccessfulEvent = BackendProjectTemplate.Contracts.Events.UserSignInSuccessful;

namespace BackendProjectTemplate.Consumer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSubscribers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILoginActivityIpAddressResolver, LoginActivityIpAddressResolver>();

        var options = configuration
            .GetSection(RabbitMqMessagingOptions.SectionName)
            .Get<RabbitMqMessagingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{RabbitMqMessagingOptions.SectionName}' is required.");

        options.Validate();

        var subscriberConfig = new SubscriberConfig
        {
            ServiceName = options.ServiceName,
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            SubscriptionName = "authentication-events",
            ExchangeName = options.EventsExchange,
            PrefetchCount = 5,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        var consumerConfig = new ConsumerConfig
        {
            ServiceName = options.ServiceName,
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            QueueName = "notifications",
            ExchangeName = options.CommandsExchange,
            PrefetchCount = 5,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        services
            .AddSubscriber(subscriberConfig, builder => builder
                .AddHandler<UserCreatedEvent, UserCreatedHandler>()
                .AddHandler<UserSignInSuccessfulEvent, UserSignInSuccessfulHandler>()
                .AddHandler<UserAccessTokenRefreshedEvent, UserAccessTokenRefreshedHandler>()
                .AddHandler<UserSignInFailedEvent, UserSignInFailedHandler>())
            .AddConsumer(consumerConfig, builder => builder
                .AddHandler<ResetPassword, ResetPasswordHandler>()
                .AddHandler<SendNotification, SendNotificationHandler>())
            .AddHostedService(serviceProvider => new Worker(
                serviceProvider.GetRequiredKeyedService<ISubscriber>(subscriberConfig.Key),
                serviceProvider.GetRequiredKeyedService<IConsumer>(consumerConfig.Key),
                serviceProvider.GetRequiredService<ILogger<Worker>>(),
                serviceProvider.GetRequiredService<WorkerReadinessState>()));

        return services;
    }
}
