using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BackendProjectTemplate.Domain.Common.Observability;

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
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "BackendProjectTemplate.WebAPI";
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        var telemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName));

        telemetry.WithTracing(tracing =>
        {
            tracing
                .AddSource(Domain.Common.Observability.Observability.ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            }
        });

        telemetry.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter();
        });

        return services;
    }
}
