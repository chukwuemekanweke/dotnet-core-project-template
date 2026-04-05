using System.Net;
using BackendProjectTemplate.Jobs.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.Jobs.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class HealthEndpointsShould(ContainersFixture fixture) : JobsIntegrationTestBase(fixture)
{
    [Fact]
    public async Task WhenCheckingLiveness_ShouldReturnHealthy()
    {
        const string livenessEndpoint = "/health/liveness";

        HttpResponseMessage response = default!;

        await WhenCheckingLiveness();
        ThenTheEndpointIsHealthy();

        async Task WhenCheckingLiveness()
        {
            response = await Client.GetAsync(livenessEndpoint);
        }

        void ThenTheEndpointIsHealthy()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task WhenCheckingReadinessWithAvailableDependencies_ShouldReturnHealthy()
    {
        const string readinessEndpoint = "/health/readiness";

        HttpResponseMessage response = default!;

        await GivenSqlServerAndRedisAreAvailable();
        await WhenCheckingReadiness();
        ThenTheEndpointIsHealthy();

        async Task GivenSqlServerAndRedisAreAvailable()
        {
            await WaitForHealthyAsync(() => Client.GetAsync(readinessEndpoint));
        }

        async Task WhenCheckingReadiness()
        {
            response = await Client.GetAsync(readinessEndpoint);
        }

        void ThenTheEndpointIsHealthy()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}
