using System.Net;
using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenCheckingReadinessWithAvailableDependencies_ShouldReturnHealthy(ContainersFixture fixture)
    : ConsumerIntegrationTestBase(fixture)
{
    private const string ReadinessEndpoint = "/health/readiness";
    private HttpResponseMessage? _response;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await WaitForDependenciesToBecomeHealthyAsync();
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Verify()
    {
        await WhenCheckingReadiness();
        ThenTheEndpointIsHealthy();

        async Task WhenCheckingReadiness()
        {
            _response = await Client.GetAsync(ReadinessEndpoint);
        }

        void ThenTheEndpointIsHealthy()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    private Task WaitForDependenciesToBecomeHealthyAsync() =>
        WaitForHealthyAsync(() => Client.GetAsync(ReadinessEndpoint));
}
