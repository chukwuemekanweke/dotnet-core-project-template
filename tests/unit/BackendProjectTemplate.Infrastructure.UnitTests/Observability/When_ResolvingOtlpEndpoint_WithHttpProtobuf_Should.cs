using BackendProjectTemplate.Infrastructure.Observability;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Observability;

public sealed class When_ResolvingOtlpEndpoint_WithHttpProtobuf_Should
{
    [Fact]
    public void AppendSignalPath()
    {
        var result = OtlpEndpointResolver.Resolve("https://example.grafana.net/otlp", "http/protobuf", "traces");

        result.ShouldBe(new Uri("https://example.grafana.net/otlp/v1/traces"));
    }
}
