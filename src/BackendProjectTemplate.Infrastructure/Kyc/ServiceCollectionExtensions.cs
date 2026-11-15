using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Infrastructure.Kyc.Kycaid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Infrastructure.Kyc;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKycServices(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(KycaidOptions.SectionName);
        var options = section.Get<KycaidOptions>() ?? new KycaidOptions();
        services.AddOptions<KycaidOptions>().Bind(section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<KycaidOptions>, KycaidOptionsValidator>();
        services.AddHttpClient<KycaidClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
        services.AddScoped<IKycProviderService>(provider => provider.GetRequiredService<KycaidClient>());
        services.AddScoped<IKycCallbackSignatureValidator, KycaidCallbackSignatureValidator>();
        return services;
    }
}
