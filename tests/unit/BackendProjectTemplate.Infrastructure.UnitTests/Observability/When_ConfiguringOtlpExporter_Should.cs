using BackendProjectTemplate.Infrastructure.Observability;
using OpenTelemetry.Exporter;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Observability;

public sealed class When_ConfiguringOtlpExporter_Should
{
    private const string GrafanaGateway = "https://otlp-gateway-prod-eu-central-0.grafana.net/otlp";

    [Fact]
    public void ApplySignalEndpointAndHeadersForHttpProtobuf()
    {
        var options = new OtlpExporterOptions();

        OtlpExporterConfigurator.Configure(options, GrafanaGateway, "http/protobuf", "Authorization=Basic abc123\n", "traces");

        options.Endpoint.ShouldBe(new Uri($"{GrafanaGateway}/v1/traces"));
        options.Headers.ShouldBe("Authorization=Basic abc123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LeaveHeadersUnsetWhenNotConfigured(string? headers)
    {
        var options = new OtlpExporterOptions();

        OtlpExporterConfigurator.Configure(options, "http://otel-collector:4317", "grpc", headers, "metrics");

        options.Endpoint.ShouldBe(new Uri("http://otel-collector:4317"));
        options.Headers.ShouldBeNullOrEmpty();
    }
}
