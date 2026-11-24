using BackendProjectTemplate.Consumer.Analytics;
using BackendProjectTemplate.Consumer.Authentication;
using BackendProjectTemplate.Consumer.Kyc;
using BackendProjectTemplate.Consumer.Notifications;
using BackendProjectTemplate.Consumer.Payments;
using BackendProjectTemplate.Consumer.Storage;
using BackendProjectTemplate.Consumer.Subscriptions;
using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Contracts.Commands.Storage;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer.DependencyInjection;
using EmailDeliveryWebhookReceivedEvent = BackendProjectTemplate.Contracts.Events.EmailDeliveryWebhookReceived;
using KycWebhookReceivedEvent = BackendProjectTemplate.Contracts.Events.KycWebhookReceived;
using ResetPassword = BackendProjectTemplate.Contracts.Commands.Authentication.ResetPasswordCommand;
using SendEmailConfirmationOtp = BackendProjectTemplate.Contracts.Commands.Authentication.SendEmailConfirmationOtpCommand;
using SendNotification = BackendProjectTemplate.Contracts.Commands.Notifications.SendNotificationCommand;
using SuccessfulPaymentConfirmedEvent = BackendProjectTemplate.Contracts.Events.SuccessfulPaymentConfirmed;
using UnsuccessfulPaymentConcludedEvent = BackendProjectTemplate.Contracts.Events.UnsuccessfulPaymentConcluded;
using UserAccessTokenRefreshedEvent = BackendProjectTemplate.Contracts.Events.UserAccessTokenRefreshed;
using UserActivityObservedEvent = BackendProjectTemplate.Contracts.Events.UserActivityObserved;
using UserCreatedEvent = BackendProjectTemplate.Contracts.Events.UserCreated;
using UserEmailConfirmedEvent = BackendProjectTemplate.Contracts.Events.UserEmailConfirmed;
using UserSecurityActivityOccurredEvent = BackendProjectTemplate.Contracts.Events.UserSecurityActivityOccurred;
using UserSignInFailedEvent = BackendProjectTemplate.Contracts.Events.UserSignInFailed;
using UserSignInSuccessfulEvent = BackendProjectTemplate.Contracts.Events.UserSignInSuccessful;

namespace BackendProjectTemplate.Consumer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSubscribers(
        this IServiceCollection services,
        IConfiguration configuration,
        string hostEnvironmentName)
    {
        services.AddScoped<ISecurityActivityIpAddressResolver, SecurityActivityIpAddressResolver>();
        services.AddScoped<EmailConfirmationOtpSender>();
        services.Configure<SubscriptionRenewalOptions>(configuration.GetSection(SubscriptionRenewalOptions.SectionName));
        services.AddScoped<SubscriptionRenewalCharger>();

        var options = configuration
            .GetSection(RabbitMqMessagingOptions.SectionName)
            .Get<RabbitMqMessagingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{RabbitMqMessagingOptions.SectionName}' is required.");

        options.Validate();
        var topology = RabbitMqTopology.Create(options, hostEnvironmentName);
        services.AddSingleton(topology);

        var subscriberConfig = new SubscriberConfig
        {
            ServiceName = options.ServiceName,
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            SubscriptionName = topology.EventsSubscriptionName,
            ExchangeName = topology.EventsExchange,
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
            QueueName = topology.CommandsQueueName,
            ExchangeName = topology.CommandsExchange,
            PrefetchCount = 5,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        services
            .AddSubscriber(subscriberConfig, builder => builder
                .AddHandler<UserCreatedEvent, UserCreatedHandler>()
                .AddHandler<UserEmailConfirmedEvent, UserEmailConfirmedHandler>()
                .AddHandler<UserSignInSuccessfulEvent, UserSignInSuccessfulHandler>()
                .AddHandler<UserActivityObservedEvent, UserActivityObservedHandler>()
                .AddHandler<UserAccessTokenRefreshedEvent, UserAccessTokenRefreshedHandler>()
                .AddHandler<UserSignInFailedEvent, UserSignInFailedHandler>()
                .AddHandler<UserSecurityActivityOccurredEvent, UserSecurityActivityOccurredHandler>()
                .AddHandler<EmailDeliveryWebhookReceivedEvent, EmailDeliveryWebhookReceivedHandler>()
                .AddHandler<KycWebhookReceivedEvent, KycWebhookReceivedHandler>()
                .AddHandler<SuccessfulPaymentConfirmedEvent, SuccessfulPaymentConfirmedHandler>()
                .AddHandler<UnsuccessfulPaymentConcludedEvent, UnsuccessfulPaymentConcludedHandler>())
            .AddConsumer(consumerConfig, builder => builder
                .AddHandler<ResetPassword, ResetPasswordHandler>()
                .AddHandler<SendEmailConfirmationOtp, SendEmailConfirmationOtpHandler>()
                .AddHandler<SendNotification, SendNotificationHandler>()
                .AddHandler<CreditWalletCommand, CreditWalletHandler>()
                .AddHandler<ActivateSubscriptionCommand, ActivateSubscriptionHandler>()
                .AddHandler<RenewSubscriptionCommand, RenewSubscriptionHandler>()
                .AddHandler<RetrySubscriptionRenewalCommand, RetrySubscriptionRenewalHandler>()
                .AddHandler<ReconcileSubscriptionRenewalCommand, ReconcileSubscriptionRenewalHandler>()
                .AddHandler<SendSubscriptionRenewalReminderCommand, SendSubscriptionRenewalReminderHandler>()
                .AddHandler<ExpireSubscriptionCommand, ExpireSubscriptionHandler>()
                .AddHandler<ActivateAllocatedSubscriptionCommand, ActivateAllocatedSubscriptionHandler>()
                .AddHandler<DeleteQuarantinedObject, DeleteQuarantinedObjectHandler>())
            .AddHostedService(serviceProvider => new Worker(
                serviceProvider.GetRequiredKeyedService<ISubscriber>(subscriberConfig.Key),
                serviceProvider.GetRequiredKeyedService<IConsumer>(consumerConfig.Key),
                serviceProvider.GetRequiredService<ILogger<Worker>>(),
                serviceProvider.GetRequiredService<WorkerReadinessState>(),
                serviceProvider.GetRequiredService<TimeProvider>()));

        return services;
    }
}
