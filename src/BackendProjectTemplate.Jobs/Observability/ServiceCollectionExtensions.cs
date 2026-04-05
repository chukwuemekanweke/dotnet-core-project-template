using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BackendProjectTemplate.Jobs.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobsOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("BackendProjectTemplate.Jobs"))
            .WithTracing(tracing =>
            {
                tracing.AddHttpClientInstrumentation();

                var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            });

        return services;
    }
}
