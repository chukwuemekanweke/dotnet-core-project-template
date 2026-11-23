using OpenTelemetry.Exporter;

namespace BackendProjectTemplate.Infrastructure.Observability;

/// <summary>
/// Applies the OTLP endpoint and, when configured, the OTLP headers (for example the Grafana Cloud
/// "Authorization=Basic ..." header) to an exporter. Headers come from configuration key
/// <c>OTEL_EXPORTER_OTLP_HEADERS</c>: an environment variable locally, or a file-delivered runtime secret on
/// deployed hosts, where credentials must never be environment variables.
/// </summary>
public static class OtlpExporterConfigurator
{
    public const string HeadersConfigurationKey = "OTEL_EXPORTER_OTLP_HEADERS";

    public static void Configure(OtlpExporterOptions options, string endpoint, string? protocol, string? headers, string signal)
    {
        options.Endpoint = OtlpEndpointResolver.Resolve(endpoint, protocol, signal);

        if (!string.IsNullOrWhiteSpace(headers))
        {
            options.Headers = headers.Trim();
        }
    }
}
