using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Infrastructure.Payments.Credo;
using BackendProjectTemplate.Infrastructure.Payments.SafeHaven;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Payments;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        var safeHavenOptions = configuration.GetSection(SafeHavenOptions.SectionName).Get<SafeHavenOptions>() ?? new SafeHavenOptions();
        var credoOptions = configuration.GetSection(CredoOptions.SectionName).Get<CredoOptions>() ?? new CredoOptions();

        services.Configure<SafeHavenOptions>(configuration.GetSection(SafeHavenOptions.SectionName));
        services.Configure<CredoOptions>(configuration.GetSection(CredoOptions.SectionName));

        services.AddHttpClient(PaymentHttpClientNames.SafeHaven, client =>
        {
            client.BaseAddress = new Uri(safeHavenOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient(PaymentHttpClientNames.Credo, client =>
        {
            client.BaseAddress = new Uri(credoOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<ISafeHavenClient, SafeHavenClient>();
        services.AddScoped<ICredoClient, CredoClient>();
        services.AddScoped<IPaymentProviderService, SafeHavenPaymentProviderService>();
        services.AddScoped<IPaymentProviderService, CredoPaymentProviderService>();

        return services;
    }
}
