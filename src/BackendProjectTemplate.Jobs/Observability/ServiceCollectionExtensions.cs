using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;

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

        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "BackendProjectTemplate.Jobs";
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
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
        }

        return services;
    }
}
