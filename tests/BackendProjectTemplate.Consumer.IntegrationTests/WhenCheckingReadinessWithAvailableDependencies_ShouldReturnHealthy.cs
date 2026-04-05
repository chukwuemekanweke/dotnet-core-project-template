using System.Net;
using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenCheckingReadinessWithAvailableDependencies_ShouldReturnHealthy(ContainersFixture fixture)
    : ConsumerIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string ReadinessEndpoint = "/health/readiness";
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        await GivenSqlServerAndRedisAreAvailable();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DisposeClientAsync();
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

    private Task GivenSqlServerAndRedisAreAvailable() =>
        WaitForHealthyAsync(() => Client.GetAsync(ReadinessEndpoint));
}
