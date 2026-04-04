using System.Net;
using System.Net.Http.Json;
using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests;

public sealed class HealthEndpointsTests(CustomConsumerApplicationFactory factory) : IClassFixture<CustomConsumerApplicationFactory>
{
    [Fact]
    public async Task Liveness_ReturnsHealthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/liveness");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Readiness_ReturnsHealthy_WhenSqlServerAndRedisAreAvailable()
    {
        var client = factory.CreateClient();

        await WaitForHealthyAsync(() => client.GetAsync("/health/readiness"));

        var response = await client.GetAsync("/health/readiness");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task WaitForHealthyAsync(Func<Task<HttpResponseMessage>> probe)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var response = await probe();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new InvalidOperationException("The health endpoint did not become healthy in time.");
    }
}
