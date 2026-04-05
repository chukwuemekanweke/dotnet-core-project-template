using Microsoft.AspNetCore.Mvc.Testing;

namespace BackendProjectTemplate.Jobs.IntegrationTests.Infrastructure;

public abstract class JobsIntegrationTestBase : IAsyncLifetime
{
    private readonly CustomJobsApplicationFactory _factory;

    protected JobsIntegrationTestBase(ContainersFixture fixture)
    {
        _factory = new CustomJobsApplicationFactory(fixture.SqlConnectionString, fixture.RedisConnectionString);
    }

    protected HttpClient Client { get; private set; } = default!;

    public virtual Task InitializeAsync()
    {
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    protected static async Task WaitForHealthyAsync(Func<Task<HttpResponseMessage>> probe)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var response = await probe();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new InvalidOperationException("The health endpoint did not become healthy in time.");
    }
}
