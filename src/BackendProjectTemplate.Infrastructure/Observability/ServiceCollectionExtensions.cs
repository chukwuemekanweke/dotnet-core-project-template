using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BackendProjectTemplate.Domain.Common.Observability;
using OpenTelemetry.Logs;

namespace BackendProjectTemplate.Infrastructure.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomTelemetryContext(this IServiceCollection services)
    {
        services.AddSingleton<ICustomTelemetryContext, CustomTelemetryContext>();
        return services;
    }

    public static IServiceCollection AddBackendTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"]
            ?? throw new InvalidOperationException("Configuration value 'OpenTelemetry:ServiceName' is required.");
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"]
            ?? throw new InvalidOperationException("Configuration value 'OpenTelemetry:OtlpEndpoint' is required.");

        var telemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName));

        telemetry.WithTracing(tracing =>
        {
            tracing
                .AddSource(Domain.Common.Observability.Observability.ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        });

        telemetry.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter();
        });

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = false;
                options.ParseStateValues = true;
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
                options.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = new Uri(otlpEndpoint));
            });
        });

        return services;
    }
}
