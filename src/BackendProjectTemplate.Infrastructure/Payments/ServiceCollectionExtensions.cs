using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Infrastructure.Http;
using BackendProjectTemplate.Infrastructure.Payments.Bachs;
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
        var bachsOptions = configuration.GetSection(BachsOptions.SectionName).Get<BachsOptions>() ?? new BachsOptions();

        services.Configure<SafeHavenOptions>(configuration.GetSection(SafeHavenOptions.SectionName));
        services.Configure<CredoOptions>(configuration.GetSection(CredoOptions.SectionName));
        services.Configure<BachsOptions>(configuration.GetSection(BachsOptions.SectionName));

        // Checkout creation is intentionally not covered by the generic transient retry policy.
        // Bachs idempotency makes an explicit retry safe, but an uncertain write must be reconciled first.
        services.AddHttpClient(PaymentHttpClientNames.Bachs, client =>
        {
            client.BaseAddress = new Uri(bachsOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient(PaymentHttpClientNames.BachsRead, client =>
        {
            client.BaseAddress = new Uri(bachsOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpRetryPolicies.CreateTransientRetryPolicy());

        services.AddHttpClient(PaymentHttpClientNames.SafeHaven, client =>
        {
            client.BaseAddress = new Uri(safeHavenOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpRetryPolicies.CreateTransientRetryPolicy());
        services.AddHttpClient(PaymentHttpClientNames.Credo, client =>
        {
            client.BaseAddress = new Uri(credoOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(HttpRetryPolicies.CreateTransientRetryPolicy());

        services.AddScoped<ISafeHavenClient, SafeHavenClient>();
        services.AddScoped<ICredoClient, CredoClient>();
        services.AddScoped<IBachsClient, BachsClient>();
        services.AddScoped<ICredoWebhookSignatureValidator, CredoWebhookSignatureValidator>();
        services.AddScoped<IBachsWebhookSignatureValidator, BachsWebhookSignatureValidator>();
        services.AddScoped<IPaymentProviderService, SafeHavenPaymentProviderService>();
        services.AddScoped<IPaymentProviderService, CredoPaymentProviderService>();
        services.AddScoped<IPaymentProviderService, BachsPaymentProviderService>();

        return services;
    }
}
