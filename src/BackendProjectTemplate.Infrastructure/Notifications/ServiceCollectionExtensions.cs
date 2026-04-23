using BackendProjectTemplate.Domain.Common.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Notifications;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(EmailNotificationsOptions.SectionName)
            .Get<EmailNotificationsOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{EmailNotificationsOptions.SectionName}' is required.");

        options.Validate();

        services.Configure<EmailNotificationsOptions>(configuration.GetSection(EmailNotificationsOptions.SectionName));
        services.AddScoped<IEmailNotificationService, EmailNotificationDispatcher>();
        services.AddScoped<IEmailTransportProvider, MailtrapEmailTransportProvider>();

        return services;
    }
}
